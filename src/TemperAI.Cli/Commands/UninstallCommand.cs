using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using TemperAI.Installer;

namespace TemperAI.Cli.Commands;

public sealed class UninstallSettings : CommandSettings
{
    [CommandOption("--dry-run")]
    [Description("Muestra que se desinstalara sin ejecutar cambios")]
    public bool DryRun { get; init; }

    [CommandOption("-f|--force")]
    [Description("Saltea la confirmacion y desinstala directamente")]
    public bool Force { get; init; }

    [CommandOption("--no-cli")]
    [Description("No desinstala el CLI (solo skills, agents y NeuralCore)")]
    public bool NoCli { get; init; }

    [CommandOption("--no-path")]
    [Description("No remueve del PATH")]
    public bool NoPath { get; init; }

    [CommandOption("-a|--agent")]
    [Description("ID del agente a desinstalar (copilot, claude, opencode). Por defecto: todos")]
    public string? AgentId { get; init; }
}

public sealed class UninstallCommand : Command<UninstallSettings>
{
    public override int Execute(CommandContext context, UninstallSettings settings)
    {
        PrintHeader();

        if (settings.DryRun)
        {
            return ExecuteDryRun(settings);
        }

        return ExecuteUninstall(settings);
    }

    private int ExecuteDryRun(UninstallSettings settings)
    {
        AnsiConsole.MarkupLine("[yellow]  Modo dry-run — no se eliminara nada[/]");
        AnsiConsole.WriteLine();

        UninstallerService service = new(dryRun: true);
        List<string> plannedDeletions = service.GetPlannedDeletions();

        if (plannedDeletions.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No se encontraron componentes de TemperAI para desinstalar.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine("[bold]Lo siguiente seria eliminado:[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Componente[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Estado[/]").Centered());

        foreach (string deletion in plannedDeletions)
        {
            string status = "[yellow]pendiente[/]";
            table.AddRow(deletion.EscapeMarkup(), status);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total: {plannedDeletions.Count} archivos/entradas[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private int ExecuteUninstall(UninstallSettings settings)
    {
        // Show what will be uninstalled
        UninstallerService service = new(dryRun: false);
        List<string> plannedDeletions = service.GetPlannedDeletions();

        if (plannedDeletions.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No se encontraron componentes de TemperAI instalados.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine("[bold]Componentes a desinstalar:[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn(new TableColumn("[bold]Componente[/]").LeftAligned());

        foreach (string deletion in plannedDeletions)
        {
            table.AddRow(deletion.EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total: {plannedDeletions.Count} archivos/entradas[/]");
        AnsiConsole.WriteLine();

        // Ask for confirmation unless --force
        if (!settings.Force)
        {
            bool confirm = AnsiConsole.Confirm(
                "[bold red]¿Estas seguro que queres desinstalar TemperAI completamente?[/]",
                defaultValue: false);

            if (!confirm)
            {
                AnsiConsole.MarkupLine("[dim]Desinstalacion cancelada.[/]");
                AnsiConsole.WriteLine();
                return 0;
            }

            // Double confirmation
            bool reallySure = AnsiConsole.Confirm(
                "[yellow]Esta accion no se puede deshacer automaticamente. ¿Continuar?[/]",
                defaultValue: false);

            if (!reallySure)
            {
                AnsiConsole.MarkupLine("[dim]Desinstalacion cancelada.[/]");
                AnsiConsole.WriteLine();
                return 0;
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Desinstalando TemperAI...[/]");
        AnsiConsole.WriteLine();

        var progress = AnsiConsole.Progress();
        progress.AutoClear = true;
        progress.Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new SpinnerColumn(),
        });

        var results = new List<UninstallResult>();

        progress.Start(ctx =>
        {
            // Step 1: Uninstall skills & agents from agent targets
            ProgressTask? agentTask = ctx.AddTask("[cyan]Removiendo skills y agents...[/]");

            IReadOnlyList<AgentTarget> targets = ResolveTargets(settings);

            foreach (AgentTarget target in targets)
            {
                agentTask.Description = $"[cyan]Removiendo de {target.Name}...[/]";

                UninstallerService perAgentService = new(dryRun: false);
                UninstallResult agentResult = perAgentService.UninstallAgent(target);
                results.Add(agentResult);

                agentTask.Increment(1.0 / targets.Count);
            }

            agentTask.Description = "[green]Skills y agents removidos[/]";
            agentTask.Value = agentTask.MaxValue;

            // Step 2: Uninstall NeuralCore
            ProgressTask? neuralTask = ctx.AddTask("[cyan]Removiendo NeuralCore...[/]");
            UninstallerService neuralService = new(dryRun: false);
            UninstallResult neuralResult = neuralService.UninstallNeuralCore();
            results.Add(neuralResult);
            neuralTask.Value = neuralTask.MaxValue;

            // Step 3: Remove from PATH
            ProgressTask? pathTask = ctx.AddTask("[cyan]Removiendo del PATH...[/]");

            if (!settings.NoPath)
            {
                UninstallerService pathService = new(dryRun: false);
                UninstallResult pathResult = pathService.RemoveFromPath();
                results.Add(pathResult);
            }
            else
            {
                pathTask.Description = "[dim]PATH (omitido por --no-path)[/]";
            }

            pathTask.Value = pathTask.MaxValue;

            // Step 4: Uninstall CLI (must be last)
            ProgressTask? cliTask = ctx.AddTask("[cyan]Removiendo CLI...[/]");

            if (!settings.NoCli)
            {
                UninstallerService cliService = new(dryRun: false);
                UninstallResult cliResult = cliService.UninstallCli();
                results.Add(cliResult);
            }
            else
            {
                cliTask.Description = "[dim]CLI (omitido por --no-cli)[/]";
            }

            cliTask.Value = cliTask.MaxValue;
        });

        // Print results
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        PrintResults(results);

        bool anyErrors = results.Any(r => r.Errors.Count > 0);

        if (!anyErrors)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green bold]✓ TemperAI desinstalado correctamente.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Reiniciá tu terminal para que los cambios en el PATH surtan efecto.[/]");

            if (settings.NoCli)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Nota: El CLI no fue removido (--no-cli). Para completar la desinstalacion, ejecutá:[/]");
                AnsiConsole.MarkupLine($"  [bold]del \"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Programs\\TemperAI\\temper-ai.exe\"[/]");
            }
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow bold]⚠ Desinstalacion completada con errores.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Algunos componentes no pudieron ser removidos. Podés intentar manualmente.[/]");
        }

        AnsiConsole.WriteLine();

        return anyErrors ? 1 : 0;
    }

    private static IReadOnlyList<AgentTarget> ResolveTargets(UninstallSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.AgentId))
        {
            if (settings.AgentId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return AgentTargets.Supported();
            }

            AgentTarget? target = AgentTargets.FindById(settings.AgentId);

            if (target is null)
            {
                AnsiConsole.MarkupLine($"[red]Agente no encontrado: {settings.AgentId}[/]");
                return Array.Empty<AgentTarget>();
            }

            return new[] { target };
        }

        return AgentTargets.Supported();
    }

    private static void PrintResults(List<UninstallResult> results)
    {
        foreach (UninstallResult result in results)
        {
            AnsiConsole.MarkupLine($"[bold]{result.Component}[/]");

            if (result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"  [green]✓[/] {result.Summary()}");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [red]✗[/] {result.Summary()}");
            }

            foreach (string removed in result.Removed)
            {
                AnsiConsole.MarkupLine($"  [green]-[/] {removed.EscapeMarkup()}");
            }

            foreach (string skipped in result.Skipped)
            {
                AnsiConsole.MarkupLine($"  [dim]~ {skipped.EscapeMarkup()}[/]");
            }

            foreach (string error in result.Errors)
            {
                AnsiConsole.MarkupLine($"  [red]✗[/] {error.EscapeMarkup()}");
            }

            AnsiConsole.WriteLine();
        }
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Desinstalador — Remueve TemperAI completamente[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
