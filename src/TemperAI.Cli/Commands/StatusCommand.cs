using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Assets;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using System.ComponentModel;
using System.Reflection;

namespace TemperAI.Cli.Commands;

public sealed class StatusSettings : CommandSettings
{
}

public sealed class StatusCommand : Command<StatusSettings>
{
    private static readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md"
    };

    public override int Execute(CommandContext context, StatusSettings settings)
    {
        PrintHeader();

        string version = GetVersion();
        AnsiConsole.MarkupLine($"[bold]Version:[/] {version}");
        AnsiConsole.WriteLine();

        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.All();

        Table agentTable = new Table();
        agentTable.AddColumn(new TableColumn("[bold]Agente[/]"));
        agentTable.AddColumn(new TableColumn("[bold]Estado[/]"));
        agentTable.AddColumn(new TableColumn("[bold]Skills[/]"));
        agentTable.AddColumn(new TableColumn("[bold]Agents[/]"));
        agentTable.AddColumn(new TableColumn("[bold]Faltantes[/]"));

        int totalMissing = 0;

        foreach (AgentTarget target in supportedTargets)
        {
            bool skillsInstalled = Directory.Exists(target.SkillsPath);
            bool agentsInstalled = Directory.Exists(target.AgentsPath);

            int installedSkills = skillsInstalled
                ? CountInstalledFiles(target.SkillsPath)
                : 0;

            int installedAgents = agentsInstalled
                ? CountInstalledFiles(target.AgentsPath)
                : 0;

            int totalSkills = CountEmbeddedFiles("skills");
            int totalAgents = CountEmbeddedFiles("agents");

            int missingSkills = skillsInstalled
                ? totalSkills - installedSkills
                : totalSkills;

            int missingAgents = agentsInstalled
                ? totalAgents - installedAgents
                : totalAgents;

            int missing = Math.Max(0, missingSkills) + Math.Max(0, missingAgents);
            totalMissing += missing;

            string status = skillsInstalled && agentsInstalled
                ? "[green]instalado[/]"
                : skillsInstalled || agentsInstalled
                    ? "[yellow]parcial[/]"
                    : "[red]no instalado[/]";

            string skillsInfo = skillsInstalled
                ? $"{installedSkills}/{totalSkills}"
                : $"0/{totalSkills}";

            string agentsInfo = agentsInstalled
                ? $"{installedAgents}/{totalAgents}"
                : $"0/{totalAgents}";

            string missingInfo = missing > 0
                ? $"[yellow]{missing}[/]"
                : "[green]—[/]";

            agentTable.AddRow(
                target.Name,
                status,
                skillsInfo,
                agentsInfo,
                missingInfo);
        }

        AnsiConsole.Write(agentTable);
        AnsiConsole.WriteLine();

        if (totalMissing > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Hay {totalMissing} archivo(s) que no están instalados.[/]");
            AnsiConsole.MarkupLine("[dim]Ejecutá [bold]temper-ai install[/] para instalar o [bold]temper-ai update[/] para actualizar.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Todo está instalado y actualizado.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static int CountInstalledFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        int count = 0;

        foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(file);

            if (!_ignoredFiles.Contains(fileName))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountEmbeddedFiles(string prefix)
    {
        IReadOnlyList<string> paths = EmbeddedAssets.ListPaths(prefix);

        int count = 0;

        foreach (string path in paths)
        {
            string fileName = Path.GetFileName(path);

            if (!_ignoredFiles.Contains(fileName))
            {
                count++;
            }
        }

        return count;
    }

    private static string GetVersion()
    {
        Assembly assembly = typeof(StatusCommand).Assembly;
        Version? version = assembly.GetName().Version;

        if (version is null)
        {
            return "desconocida";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}";
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
