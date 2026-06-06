using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Configuration;
using TemperAI.Installer;

namespace TemperAI.Cli.Commands;

public sealed class StatusSettings : CommandSettings
{
}

public sealed class StatusCommand : Command<StatusSettings>
{
    public override int Execute(CommandContext context, StatusSettings settings)
    {
        PrintHeader();

        InstallMetadataService metadataService = new();
        var metadata = metadataService.Load();

        Table summaryTable = new();
        summaryTable.AddColumn("[bold]Clave[/]");
        summaryTable.AddColumn("[bold]Valor[/]");

        summaryTable.AddRow("CLI version", GetVersion());
        summaryTable.AddRow("Install root", InstallationPaths.InstallRoot);
        summaryTable.AddRow("Source mode", metadata?.SourceMode ?? "desconocido");
        summaryTable.AddRow("Channel", metadata?.Channel ?? "stable");
        summaryTable.AddRow("Assets version", metadata?.InstalledAssetsVersion ?? "desconocida");

        AnsiConsole.Write(summaryTable);
        AnsiConsole.WriteLine();

        Table targetsTable = new();
        targetsTable.AddColumn("[bold]Agente[/]");
        targetsTable.AddColumn("[bold]Skills path[/]");
        targetsTable.AddColumn("[bold]Agents path[/]");
        targetsTable.AddColumn("[bold]Estado[/]");

        foreach (var target in AgentTargets.Supported())
        {
            bool skillsExists = Directory.Exists(target.SkillsPath);
            bool agentsExists = Directory.Exists(target.AgentsPath);
            string status = skillsExists && agentsExists
                ? "[green]instalado[/]"
                : skillsExists || agentsExists
                    ? "[yellow]parcial[/]"
                    : "[red]no instalado[/]";

            targetsTable.AddRow(target.Name, target.SkillsPath, target.AgentsPath, status);
        }

        AnsiConsole.Write(targetsTable);
        AnsiConsole.WriteLine();

        return 0;
    }

    private static string GetVersion()
    {
        Assembly assembly = typeof(StatusCommand).Assembly;
        Version? version = assembly.GetName().Version;
        return version?.ToString() ?? "desconocida";
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Estado del ecosistema AI instalado[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
