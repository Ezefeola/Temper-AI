using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Cli.Services;
using TemperAI.Core.Models;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class DoctorSettings : CommandSettings
{
}

public sealed class DoctorCommand : Command<DoctorSettings>
{
    public override int Execute(CommandContext context, DoctorSettings settings)
    {
        PrintHeader();

        AnsiConsole.MarkupLine("[bold]Ejecutando diagnóstico completo...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        DoctorResult result = service.RunDoctor();

        foreach (DoctorCheckResult check in result.Checks)
        {
            string statusIcon = check.Passed ? "[green]✓[/]" : "[red]✗[/]";
            string statusText = check.Passed ? "PASS" : "FAIL";

            AnsiConsole.MarkupLine($"{statusIcon} [bold]{check.CheckName}:[/] [{(check.Passed ? "green" : "red")}]{statusText}[/]");

            if (!string.IsNullOrEmpty(check.Details))
            {
                AnsiConsole.MarkupLine($"    [dim]{check.Details}[/]");
            }

            if (!check.Passed && check.FixSuggestion is not null)
            {
                AnsiConsole.MarkupLine($"    [yellow]💡 {check.FixSuggestion}[/]");
            }

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        if (result.AllPassed)
        {
            AnsiConsole.MarkupLine("[bold green]✓ Todos los checks pasaron![/]");
            AnsiConsole.MarkupLine("[dim]Tu instalación de TemperAI está funcionando correctamente.[/]");
        }
        else
        {
            int failedCount = result.Checks.Count(c => !c.Passed);
            AnsiConsole.MarkupLine($"[bold yellow]⚠ {failedCount} check(s) fallaron[/]");
            AnsiConsole.WriteLine();

            if (result.CanRepair)
            {
                bool fixIt = AnsiConsole.Confirm("¿Querés que intente reparar automáticamente los problemas?");

                if (fixIt)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
                    AnsiConsole.MarkupLine("[bold]Ejecutando reparaciones...[/]");
                    AnsiConsole.WriteLine();

                    RepairResult repairResult = service.Repair();

                    if (repairResult.Success)
                    {
                        AnsiConsole.MarkupLine("[bold green]✓ Reparaciones completadas[/]");
                        AnsiConsole.WriteLine();

                        foreach (string action in repairResult.ActionsPerformed)
                        {
                            AnsiConsole.MarkupLine($"  [green]•[/] {action}");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[bold red]✗ Algunas reparaciones fallaron[/]");
                        AnsiConsole.WriteLine();

                        foreach (string error in repairResult.Errors)
                        {
                            AnsiConsole.MarkupLine($"  [red]•[/] {error}");
                        }
                    }
                }
            }
        }

        AnsiConsole.WriteLine();
        return result.AllPassed ? 0 : 1;
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("Doctor")
                .Color(Color.Green));

        AnsiConsole.MarkupLine("[dim]Diagnóstico de instalación TemperAI[/]");
        AnsiConsole.WriteLine();
    }
}