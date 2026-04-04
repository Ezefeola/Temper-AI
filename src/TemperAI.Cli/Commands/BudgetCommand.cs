using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class BudgetSettings : CommandSettings
{
    [CommandOption("--reset")]
    [Description("Resetea el tracking de presupuesto")]
    public bool Reset { get; init; }
}

public sealed class BudgetCommand : Command<BudgetSettings>
{
    private const string BudgetFileName = "budget.md";
    private const string TemperDir = ".temper";

    public override int Execute(CommandContext context, BudgetSettings settings)
    {
        PrintHeader();

        if (settings.Reset)
        {
            ResetBudget();
            return 0;
        }

        string budgetFilePath = Path.Combine(TemperDir, BudgetFileName);

        if (!File.Exists(budgetFilePath))
        {
            AnsiConsole.MarkupLine("[yellow]No se encontro archivo de presupuesto.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Ejecutá [bold]temper-ai init[/] para iniciar un proyecto y generar el presupuesto.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        string content = File.ReadAllText(budgetFilePath);

        DisplayBudget(content);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static void DisplayBudget(string content)
    {
        var phaseUsages = ParsePhaseUsage(content);
        var config = ParseConfiguration(content);

        if (config.Count > 0)
        {
            DisplayConfiguration(config);
        }

        if (phaseUsages.Count > 0)
        {
            DisplayPhaseTable(phaseUsages);
            DisplaySummary(phaseUsages);
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]No hay fases registradas todavia.[/]");
        }
    }

    private static void DisplayConfiguration(Dictionary<string, string> config)
    {
        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Configuracion[/]"));
        table.AddColumn(new TableColumn("[bold]Valor[/]"));

        foreach (var entry in config)
        {
            table.AddRow(entry.Key, entry.Value);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayPhaseTable(List<PhaseUsage> usages)
    {
        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Fase[/]"));
        table.AddColumn(new TableColumn("[bold]Est. Input[/]"));
        table.AddColumn(new TableColumn("[bold]Est. Output[/]"));
        table.AddColumn(new TableColumn("[bold]Est. Total[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));

        foreach (PhaseUsage usage in usages)
        {
            string status = usage.Status.ToLowerInvariant() switch
            {
                "done" => "[green]Done[/]",
                "in-progress" => "[yellow]In Progress[/]",
                "pending" => "[dim]Pending[/]",
                _ => usage.Status
            };

            table.AddRow(
                usage.Phase,
                usage.EstimatedIn == 0 ? "[dim]—[/]" : usage.EstimatedIn.ToString(),
                usage.EstimatedOut == 0 ? "[dim]—[/]" : usage.EstimatedOut.ToString(),
                usage.EstimatedTotal == 0 ? "[dim]—[/]" : usage.EstimatedTotal.ToString(),
                status);
        }

        AnsiConsole.Write(table);
    }

    private static void DisplaySummary(List<PhaseUsage> usages)
    {
        int totalUsed = usages
            .Where(u => u.Status.Equals("done", StringComparison.OrdinalIgnoreCase))
            .Sum(u => u.EstimatedTotal);

        int pendingPhases = usages
            .Count(u => u.Status.Equals("pending", StringComparison.OrdinalIgnoreCase));

        int maxTotal = 83000;
        double utilization = maxTotal > 0 ? (double)totalUsed / maxTotal * 100 : 0;

        string utilizationColor = utilization >= 80 ? "red" : utilization >= 50 ? "yellow" : "green";

        AnsiConsole.MarkupLine($"[bold]Resumen:[/]");
        AnsiConsole.MarkupLine($"  Tokens usados:     [bold]{totalUsed:N0}[/]");
        AnsiConsole.MarkupLine($"  Budget restante:   [bold]{maxTotal - totalUsed:N0}[/]");
        AnsiConsole.MarkupLine($"  Utilizacion:       [{utilizationColor}]{utilization:F1}%[/]");
        AnsiConsole.MarkupLine($"  Fases pendientes:  [bold]{pendingPhases}[/]");

        if (utilization >= 80)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]⚠ Alerta: Presupuesto al {utilization:F1}%. Considera optimizar las fases restantes.[/]");
        }
    }

    private static List<PhaseUsage> ParsePhaseUsage(string content)
    {
        List<PhaseUsage> usages = [];

        string[] lines = content.Split('\n');
        bool inTable = false;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("| Phase") || trimmed.StartsWith("| `temper-"))
            {
                inTable = true;
            }

            if (!inTable)
            {
                continue;
            }

            if (trimmed.StartsWith("|---"))
            {
                continue;
            }

            if (!trimmed.StartsWith("|"))
            {
                if (inTable)
                {
                    inTable = false;
                }

                continue;
            }

            string[] parts = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 5)
            {
                continue;
            }

            string phase = parts[0].Replace("`", "").Trim();

            if (!phase.StartsWith("temper-") && phase != "Total (full pipeline)")
            {
                continue;
            }

            int estimatedIn = ParseNumber(parts[1]);
            int estimatedOut = ParseNumber(parts[2]);
            int estimatedTotal = ParseNumber(parts[3]);
            string status = parts.Length > 4 ? parts[4] : "Pending";

            usages.Add(new PhaseUsage
            {
                Phase = phase,
                EstimatedIn = estimatedIn,
                EstimatedOut = estimatedOut,
                EstimatedTotal = estimatedTotal,
                Status = status
            });
        }

        return usages;
    }

    private static Dictionary<string, string> ParseConfiguration(string content)
    {
        Dictionary<string, string> config = [];

        string[] lines = content.Split('\n');
        bool inTable = false;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("| Max tokens") || trimmed.StartsWith("| Alert") || trimmed.StartsWith("| Hard"))
            {
                inTable = true;
            }

            if (!inTable)
            {
                continue;
            }

            if (trimmed.StartsWith("|---"))
            {
                continue;
            }

            if (!trimmed.StartsWith("|"))
            {
                inTable = false;
                continue;
            }

            string[] parts = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2)
            {
                config[parts[0]] = parts[1];
            }
        }

        return config;
    }

    private static int ParseNumber(string text)
    {
        text = text.Replace(",", "").Replace("—", "").Replace("-", "").Trim();

        if (int.TryParse(text, out int result))
        {
            return result;
        }

        return 0;
    }

    private static void ResetBudget()
    {
        string budgetFilePath = Path.Combine(TemperDir, BudgetFileName);

        if (!File.Exists(budgetFilePath))
        {
            AnsiConsole.MarkupLine("[yellow]No hay presupuesto para resetear.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        bool confirmed = AnsiConsole.Confirm("¿Seguro que queres resetear el presupuesto?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[dim]Reset cancelado.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        File.Delete(budgetFilePath);
        AnsiConsole.MarkupLine("[green]Presupuesto reseteado.[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Token Budget Tracker[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    private sealed class PhaseUsage
    {
        public string Phase { get; init; } = string.Empty;
        public int EstimatedIn { get; init; }
        public int EstimatedOut { get; init; }
        public int EstimatedTotal { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
