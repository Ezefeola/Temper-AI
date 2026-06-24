using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using TemperAI.Installer;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class InstallSettings : CommandSettings
{
    [CommandOption("--dry-run")]
    [Description("Simula la instalacion sin escribir archivos")]
    public bool DryRun { get; init; }

    [CommandOption("-a|--agent")]
    [Description("ID del agente a instalar (opencode, claude)")]
    public string? AgentId { get; init; }

    [CommandOption("--source")]
    [Description("Origen de assets: remote (default) o local")]
    public string? SourceMode { get; init; }

    [CommandOption("--neuralcore")]
    [Description("Instala NeuralCore MCP server")]
    public bool? InstallNeuralCore { get; init; }
}

public sealed class InstallCommand : Command<InstallSettings>
{
    public override int Execute(CommandContext context, InstallSettings settings)
    {
        PrintHeader();

        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.Supported();

        if (!string.IsNullOrWhiteSpace(settings.SourceMode) && !InstallSourceMode.IsValid(settings.SourceMode))
        {
            AnsiConsole.MarkupLine("[red]Source invalido. Usá 'remote' o 'local'.[/]");
            return 1;
        }

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

        bool installNeuralCore = settings.InstallNeuralCore ?? AnsiConsole.Confirm(
            "¿Queres instalar NeuralCore para memoria persistente entre sesiones?\n  → Esto publicara NeuralCore y configurara MCP en tus agentes AI",
            defaultValue: true);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]  Modo dry-run — no se escribira nada al disco[/]");
            AnsiConsole.WriteLine();
        }

        InstallerService installerService = new(settings.DryRun);
        string sourceMode = InstallSourceMode.Normalize(settings.SourceMode);

        foreach (AgentTarget agentTarget in selectedTargets)
        {
            InstallResult installResult = installerService.Install(agentTarget, sourceMode);
            PrintResult(installResult);

            if (installNeuralCore && !settings.DryRun)
            {
                string neuralCoreExe = NeuralCoreInstallerService.GetNeuralCoreExePath();

                if (!NeuralCoreInstallerService.IsPublished())
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[bold]NeuralCore no esta publicado. Publicando ahora...[/]");
                    AnsiConsole.WriteLine();

                    var progress = AnsiConsole.Progress();
                    progress.AutoClear = true;
                    progress.Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new SpinnerColumn(),
                    });

                    bool publishSuccess = false;

                    progress.Start(ctx =>
                    {
                        ProgressTask? task = null;

                        PublishResult result = NeuralCoreInstallerService.PublishNeuralCore(msg =>
                        {
                            task ??= ctx.AddTask("[cyan]Publicando NeuralCore...[/]");
                            task.Description = $"[cyan]{msg}[/]";
                            task.Value = task.MaxValue;
                        });

                        if (result.Success)
                        {
                            publishSuccess = true;
                            AnsiConsole.MarkupLine($"[green]✓[/] NeuralCore publicado ({result.Size})");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]✗ Error al publicar:[/] {result.Error}");
                        }
                    });

                    if (!publishSuccess)
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠ NeuralCore no se pudo publicar. Saltando configuracion MCP.[/]");
                        AnsiConsole.MarkupLine("[dim]   Ejecutá [bold]temper-ai neuralcore --publish[/] manualmente.[/]");
                        continue;
                    }

                    neuralCoreExe = NeuralCoreInstallerService.GetNeuralCoreExePath();
                }

                NeuralCoreInstallerService neuralCoreInstaller = new();
                string neuralCorePath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
                InstallResult neuralResult = neuralCoreInstaller.InstallNeuralCore(agentTarget, neuralCorePath);

                if (neuralResult.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[green]✓[/] NeuralCore configurado para {agentTarget.Name}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] NeuralCore config parcial para {agentTarget.Name}");

                    foreach (string error in neuralResult.Errors)
                    {
                        AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
                    }
                }
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");

        if (installNeuralCore)
        {
            AnsiConsole.MarkupLine("[green]Listo![/] Skills, agentes y NeuralCore instalados.");
            AnsiConsole.MarkupLine("[dim]NeuralCore se iniciara automaticamente cuando abras tu agente AI.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Listo![/] Abri tu agente AI y empeza a usar TemperAI.");
            AnsiConsole.MarkupLine("[dim]Para agregar NeuralCore despues, ejecutá: [bold]temper-ai neuralcore[/][/]");
        }

        AnsiConsole.WriteLine();

        return 0;
    }

    private static List<AgentTarget> ResolveTargets(
        InstallSettings settings,
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
                .Title("[bold]¿Para que agente queres instalar?[/]")
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

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Configurador de ecosistema AI para desarrolladores .NET[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintResult(InstallResult installResult)
    {
        if (installResult.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {installResult.Summary()}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {installResult.Summary()}");
        }

        foreach (string installedFile in installResult.Installed)
        {
            AnsiConsole.MarkupLine($"  [green]+[/] {installedFile}");
        }

        foreach (string skippedFile in installResult.Skipped)
        {
            AnsiConsole.MarkupLine($"  [yellow]~[/] {skippedFile} [dim](ya existia)[/]");
        }

        foreach (string error in installResult.Errors)
        {
            AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
        }
    }
}
