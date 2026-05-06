using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Incremental;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class IncrementalSettings : CommandSettings
{
    [CommandOption("--check")]
    [Description("Verifica que fases necesitan re-ejecutarse")]
    public bool Check { get; init; }

    [CommandOption("--force")]
    [Description("Re-ejecuta todas las fases sin verificar cambios")]
    public bool Force { get; init; }
}

public sealed class IncrementalCommand : Command<IncrementalSettings>
{
    private const string TemperDir = ".temper";

    public override int Execute(CommandContext context, IncrementalSettings settings)
    {
        PrintHeader();

        IncrementalUpdateService service = new();

        if (settings.Force)
        {
            AnsiConsole.MarkupLine("[yellow]Modo forzado — todas las fases se re-ejecutaran.[/]");
            AnsiConsole.WriteLine();
            ShowAllPhases();
            return 0;
        }

        if (!settings.Check)
        {
            IncrementalResult result = service.AnalyzeChanges(TemperDir);
            DisplayResult(result);
            return 0;
        }

        AnsiConsole.MarkupLine("[bold]Verificando cambios desde el ultimo snapshot...[/]");
        AnsiConsole.WriteLine();

        IncrementalResult analysisResult = service.AnalyzeChanges(TemperDir);
        DisplayAnalysisResult(analysisResult, service);

        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static void DisplayResult(IncrementalResult result)
    {
        if (!result.RequiresRerun)
        {
            AnsiConsole.MarkupLine("[green]No se detectaron cambios. No es necesario re-ejecutar fases.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]{result.Message}[/]");
        AnsiConsole.WriteLine();

        if (result.ChangedFiles.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Archivos modificados:[/]");

            foreach (string file in result.ChangedFiles)
            {
                AnsiConsole.MarkupLine($"  [yellow]~[/] {file}");
            }

            AnsiConsole.WriteLine();
        }

        if (result.AffectedPhases.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Fases que necesitan re-ejecutarse:[/]");

            foreach (string phase in result.AffectedPhases)
            {
                AnsiConsole.MarkupLine($"  [red]↻[/] {phase}");
            }

            AnsiConsole.WriteLine();
        }

        if (result.UnchangedPhases.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Fases que NO necesitan re-ejecutarse:[/]");

            foreach (string phase in result.UnchangedPhases)
            {
                AnsiConsole.MarkupLine($"  [green]✓[/] {phase}");
            }

            AnsiConsole.WriteLine();
        }
    }

    private static void DisplayAnalysisResult(IncrementalResult result, IncrementalUpdateService service)
    {
        if (!result.RequiresRerun)
        {
            AnsiConsole.MarkupLine("[green]No se detectaron cambios. No es necesario re-ejecutar fases.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Fase[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));
        table.AddColumn(new TableColumn("[bold]Razon[/]"));

        IReadOnlyList<PhaseDependency> rerunOrder = service.GetReRunOrder(result.AffectedPhases);

        foreach (PhaseDependency phase in rerunOrder)
        {
            string reason = result.ChangedFiles.Count > 0 && phase.TrackedFiles.Length > 0
                ? $"Archivo modificado: {string.Join(", ", phase.TrackedFiles)}"
                : "Dependencia de fase afectada";

            table.AddRow(
                phase.PhaseName,
                "[red]Re-ejecutar[/]",
                reason);
        }

        foreach (string phase in result.UnchangedPhases)
        {
            table.AddRow(
                phase,
                "[green]Sin cambios[/]",
                "Archivos sin modificar");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Orden de re-ejecucion: {string.Join(" → ", rerunOrder.Select(p => p.PhaseName))}[/]");
        AnsiConsole.WriteLine();
    }

    private static void ShowAllPhases()
    {
        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Fase[/]"));
        table.AddColumn(new TableColumn("[bold]Dependencias[/]"));
        table.AddColumn(new TableColumn("[bold]Archivos[/]"));

        string[] allPhases =
        [
            "temper-analyst-prd",
            "temper-analyst-spec",
            "temper-architect",
            "temper-tasks",
            "temper-plan",
            "temper-review",
            "temper-docs"
        ];

        Dictionary<string, string[]> dependencies = new()
        {
            ["temper-analyst-prd"] = [],
            ["temper-analyst-spec"] = ["temper-analyst-prd"],
            ["temper-architect"] = ["temper-analyst-spec"],
            ["temper-tasks"] = ["temper-analyst-spec", "temper-architect"],
            ["temper-plan"] = ["temper-architect", "temper-tasks"],
            ["temper-review"] = ["temper-architect", "temper-plan"],
            ["temper-docs"] = ["temper-architect", "temper-plan", "temper-review"]
        };

        Dictionary<string, string[]> files = new()
        {
            ["temper-analyst-prd"] = ["prd.md"],
            ["temper-analyst-spec"] = ["specs/INDEX.md"],
            ["temper-architect"] = ["backend-config.md", "frontend-config.md"],
            ["temper-tasks"] = ["tasks/INDEX.md"],
            ["temper-plan"] = ["build-plan.md"],
            ["temper-review"] = [],
            ["temper-docs"] = []
        };

        foreach (string phase in allPhases)
        {
            string deps = dependencies[phase].Length > 0
                ? string.Join(", ", dependencies[phase])
                : "—";

            string trackedFiles = files[phase].Length > 0
                ? string.Join(", ", files[phase])
                : "—";

            table.AddRow(phase, deps, trackedFiles);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Incremental Updates — Deteccion de cambios[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
