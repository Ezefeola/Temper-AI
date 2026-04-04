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
    [Description("ID del agente a instalar (copilot, claude, opencode)")]
    public string? AgentId { get; init; }
}

public sealed class InstallCommand : Command<InstallSettings>
{
    public override int Execute(CommandContext context, InstallSettings settings)
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
            AnsiConsole.MarkupLine("[yellow]  Modo dry-run — no se escribira nada al disco[/]");
            AnsiConsole.WriteLine();
        }

        InstallerService installerService = new(settings.DryRun);

        foreach (AgentTarget agentTarget in selectedTargets)
        {
            InstallResult installResult = installerService.Install(agentTarget);
            PrintResult(installResult);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.MarkupLine("[green]Listo![/] Abri tu agente AI y empeza a usar TemperAI.");
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
