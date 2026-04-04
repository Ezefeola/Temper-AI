using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Assets;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class UpdateSettings : CommandSettings
{
    [CommandOption("--force")]
    [Description("Actualiza sin pedir confirmación")]
    public bool Force { get; init; }

    [CommandOption("--dry-run")]
    [Description("Muestra qué se actualizaría sin escribir archivos")]
    public bool DryRun { get; init; }

    [CommandOption("-a|--agent")]
    [Description("ID del agente a actualizar (copilot, claude, opencode)")]
    public string? AgentId { get; init; }
}

public sealed class UpdateCommand : Command<UpdateSettings>
{
    private static readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md"
    };

    public override int Execute(CommandContext context, UpdateSettings settings)
    {
        PrintHeader();

        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.Supported();

        if (supportedTargets.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No se encontraron agentes soportados en este sistema.[/]");
            return 1;
        }

        List<AgentTarget> selectedTargets = ResolveTargets(settings, supportedTargets);

        if (selectedTargets.Count == 0)
        {
            return 1;
        }

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]  Modo dry-run — no se escribirá nada al disco[/]");
            AnsiConsole.WriteLine();
        }

        foreach (AgentTarget agentTarget in selectedTargets)
        {
            UpdateResult updateResult = CheckForUpdates(agentTarget);

            if (updateResult.Updatable.Count == 0)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] [bold]{agentTarget.Name}[/] — todos los archivos están actualizados.");
                AnsiConsole.WriteLine();
                continue;
            }

            PrintUpdatableFiles(updateResult);

            if (settings.DryRun)
            {
                AnsiConsole.MarkupLine($"[yellow]{updateResult.Updatable.Count}[/] archivo(s) se actualizarían en [bold]{agentTarget.Name}[/].");
                AnsiConsole.WriteLine();
                continue;
            }

            bool shouldUpdate = settings.Force || ConfirmUpdate(updateResult.Updatable.Count, agentTarget.Name);

            if (!shouldUpdate)
            {
                AnsiConsole.MarkupLine("[dim]Actualización cancelada.[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            UpdateResult appliedResult = ApplyUpdates(agentTarget, updateResult.Updatable);
            PrintUpdateResult(appliedResult);
        }

        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static UpdateResult CheckForUpdates(AgentTarget target)
    {
        List<UpdateFileInfo> updatable = [];
        List<string> errors = [];

        CheckDirectoryForUpdates("skills", target.SkillsPath, updatable, errors);
        CheckDirectoryForUpdates("agents", target.AgentsPath, updatable, errors);

        return new UpdateResult
        {
            Target = target,
            Updatable = updatable,
            Errors = errors
        };
    }

    private static void CheckDirectoryForUpdates(
        string assetPrefix,
        string destinationDirectory,
        List<UpdateFileInfo> updatable,
        List<string> errors)
    {
        IReadOnlyList<string> assetPaths = EmbeddedAssets.ListPaths(assetPrefix);

        foreach (string assetPath in assetPaths)
        {
            string fileName = Path.GetFileName(assetPath);

            if (_ignoredFiles.Contains(fileName))
            {
                continue;
            }

            string relativePath = assetPath[(assetPrefix.Length + 1)..];
            string destinationPath = Path.Combine(destinationDirectory, relativePath);

            if (!File.Exists(destinationPath))
            {
                continue;
            }

            try
            {
                var (found, embeddedContent) = EmbeddedAssets.TryReadText(assetPath);

                if (!found)
                {
                    errors.Add($"Asset no encontrado: {assetPath}");
                    continue;
                }

                string diskContent = File.ReadAllText(destinationPath);

                if (embeddedContent != diskContent)
                {
                    updatable.Add(new UpdateFileInfo
                    {
                        AssetPath = assetPath,
                        DestinationPath = destinationPath,
                        RelativePath = relativePath
                    });
                }
            }
            catch (Exception exception)
            {
                errors.Add($"{destinationPath}: {exception.Message}");
            }
        }
    }

    private static UpdateResult ApplyUpdates(
        AgentTarget target,
        List<UpdateFileInfo> updatable)
    {
        List<string> updated = [];
        List<string> errors = [];

        foreach (UpdateFileInfo fileInfo in updatable)
        {
            var (success, error) = EmbeddedAssets.CopyToDisk(fileInfo.AssetPath, fileInfo.DestinationPath);

            if (success)
            {
                updated.Add(fileInfo.RelativePath);
            }
            else
            {
                errors.Add($"{fileInfo.RelativePath}: {error}");
            }
        }

        return new UpdateResult
        {
            Target = target,
            Updatable = [],
            Updated = updated,
            Errors = errors
        };
    }

    private static List<AgentTarget> ResolveTargets(
        UpdateSettings settings,
        IReadOnlyList<AgentTarget> supportedTargets)
    {
        if (!string.IsNullOrEmpty(settings.AgentId))
        {
            if (settings.AgentId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return supportedTargets.ToList();
            }

            AgentTarget? agentTarget = AgentTargets.FindById(settings.AgentId);

            if (agentTarget is null)
            {
                AnsiConsole.MarkupLine($"[red]Agente no encontrado: {settings.AgentId}[/]");
                return [];
            }

            return [agentTarget];
        }

        List<string> choices = supportedTargets
            .Select(target => target.Name)
            .ToList();

        choices.Add("Todos");

        string selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]¿Para qué agente querés verificar actualizaciones?[/]")
                .AddChoices(choices));

        if (selection == "Todos")
        {
            return supportedTargets.ToList();
        }

        AgentTarget? selectedTarget = supportedTargets
            .FirstOrDefault(target => target.Name == selection);

        if (selectedTarget is null)
        {
            return [];
        }

        return [selectedTarget];
    }

    private static bool ConfirmUpdate(int count, string agentName)
    {
        return AnsiConsole.Confirm(
            $"[bold]{count}[/] archivo(s) tienen versión nueva en [bold]{agentName}[/]. ¿Querés actualizar?");
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Actualizador de skills y agentes AI[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintUpdatableFiles(UpdateResult result)
    {
        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Archivo[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));

        foreach (UpdateFileInfo fileInfo in result.Updatable)
        {
            table.AddRow(
                fileInfo.RelativePath,
                "[yellow]desactualizado[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void PrintUpdateResult(UpdateResult result)
    {
        if (result.Updated.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {result.Updated.Count} archivo(s) actualizado(s) para [bold]{result.Target.Name}[/]:");
            AnsiConsole.WriteLine();

            foreach (string updatedFile in result.Updated)
            {
                AnsiConsole.MarkupLine($"  [green]↑[/] {updatedFile}");
            }

            AnsiConsole.WriteLine();
        }

        foreach (string error in result.Errors)
        {
            AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
        }
    }

    private sealed class UpdateResult
    {
        public AgentTarget Target { get; init; } = null!;
        public List<UpdateFileInfo> Updatable { get; init; } = [];
        public List<string> Updated { get; init; } = [];
        public List<string> Errors { get; init; } = [];
    }

    private sealed class UpdateFileInfo
    {
        public string AssetPath { get; init; } = string.Empty;
        public string DestinationPath { get; init; } = string.Empty;
        public string RelativePath { get; init; } = string.Empty;
    }
}
