using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using TemperAI.Installer;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class NeuralCoreSettings : CommandSettings
{
    [CommandOption("--status")]
    [Description("Verifica el estado del servidor NeuralCore MCP")]
    public bool Status { get; init; }

    [CommandOption("--test")]
    [Description("Ejecuta un test de conectividad end-to-end")]
    public bool Test { get; init; }

    [CommandOption("--install")]
    [Description("Instala la configuracion MCP en los agentes seleccionados")]
    public bool Install { get; init; }

    [CommandOption("--publish")]
    [Description("Publica NeuralCore como ejecutable standalone")]
    public bool Publish { get; init; }

    [CommandOption("--logs")]
    [Description("Muestra los ultimos logs del servidor NeuralCore")]
    public bool Logs { get; init; }

    [CommandOption("-a|--agent")]
    [Description("ID del agente para instalacion MCP (copilot, claude, opencode)")]
    public string? AgentId { get; init; }
}

public sealed class NeuralCoreCommand : Command<NeuralCoreSettings>
{
    public override int Execute(CommandContext context, NeuralCoreSettings settings)
    {
        if (settings.Publish)
        {
            return PublishNeuralCore();
        }

        if (settings.Install)
        {
            return InstallMcpConfig(settings);
        }

        if (settings.Status)
        {
            return CheckStatus();
        }

        if (settings.Test)
        {
            return RunTest();
        }

        if (settings.Logs)
        {
            return ShowLogs();
        }

        ShowMenu();
        return 0;
    }

    private static int PublishNeuralCore()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Publicando NeuralCore...[/]");
        AnsiConsole.WriteLine();

        if (NeuralCoreInstallerService.IsPublished())
        {
            AnsiConsole.MarkupLine("[yellow]NeuralCore ya esta publicado.[/]");
            AnsiConsole.MarkupLine($"[dim]Path: {NeuralCoreInstallerService.GetNeuralCoreExePath()}[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        bool publishSuccess = false;

        AnsiConsole.Progress()
            .AutoClear(true)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new SpinnerColumn(),
            })
            .Start(ctx =>
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
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]✗ Error:[/] {result.Error}");
                }
            });

        if (publishSuccess)
        {
            string exePath = NeuralCoreInstallerService.GetNeuralCoreExePath();
            long fileSize = new FileInfo(exePath).Length;
            string sizeStr = fileSize > 1_000_000 ? $"{fileSize / 1_000_000} MB" : $"{fileSize / 1_000} KB";

            AnsiConsole.MarkupLine($"[green]✓[/] [bold]NeuralCore publicado:[/]");
            AnsiConsole.MarkupLine($"  [dim]Path:[/] {exePath}");
            AnsiConsole.MarkupLine($"  [dim]Size:[/] {sizeStr}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Ahora ejecutá [bold]temper-ai install[/] o [bold]temper-ai neuralcore --install[/][/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Publicacion fallida.[/]");
            return 1;
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static int InstallMcpConfig(NeuralCoreSettings settings)
    {
        PrintHeader();

        if (!NeuralCoreInstallerService.IsPublished())
        {
            AnsiConsole.MarkupLine("[yellow]NeuralCore no esta publicado. ¿Queres publicarlo ahora?[/]");
            bool shouldPublish = AnsiConsole.Confirm("Publicar NeuralCore", defaultValue: true);

            if (shouldPublish)
            {
                int publishResult = PublishNeuralCore();

                if (publishResult != 0)
                {
                    return 1;
                }
            }
            else
            {
                return 0;
            }
        }

        List<AgentTarget> targets = ResolveTargets(settings);

        if (targets.Count == 0)
        {
            return 1;
        }

        NeuralCoreInstallerService installer = new();
        string neuralCorePath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        int successCount = 0;

        foreach (AgentTarget target in targets)
        {
            AnsiConsole.MarkupLine($"[bold]Configurando NeuralCore para {target.Name}...[/]");

            InstallResult result = installer.InstallNeuralCore(target, neuralCorePath);

            if (result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {target.Name}: MCP configurado");
                successCount++;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {target.Name}: Error");

                foreach (string error in result.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
                }
            }

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"[bold]{successCount}/{targets.Count}[/] agente(s) configurados.");
        AnsiConsole.WriteLine();

        return successCount == targets.Count ? 0 : 1;
    }

    private static int CheckStatus()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Estado de NeuralCore[/]");
        AnsiConsole.WriteLine();

        string neuralCoreExe = NeuralCoreInstallerService.GetNeuralCoreExePath();
        bool exeExists = NeuralCoreInstallerService.IsPublished();

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Componente[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));
        table.AddColumn(new TableColumn("[bold]Detalle[/]"));

        table.AddRow(
            "NeuralCore ejecutable",
            exeExists ? "[green]Publicado[/]" : "[red]No publicado[/]",
            exeExists ? $"{neuralCoreExe} ({new FileInfo(neuralCoreExe).Length / 1_000_000} MB)" : "Ejecutá: temper-ai neuralcore --publish");

        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.Supported();

        foreach (AgentTarget target in supportedTargets)
        {
            string mcpFile = target.McpConfigFile;
            bool mcpConfigured = File.Exists(mcpFile) && IsNeuralCoreInConfig(mcpFile);

            table.AddRow(
                $"MCP: {target.Name}",
                mcpConfigured ? "[green]Configurado[/]" : "[yellow]No configurado[/]",
                mcpConfigured ? mcpFile : $"Ejecutá: temper-ai neuralcore --install --agent {target.Id}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        bool allConfigured = exeExists && supportedTargets.All(t =>
            File.Exists(t.McpConfigFile) && IsNeuralCoreInConfig(t.McpConfigFile));

        if (allConfigured)
        {
            AnsiConsole.MarkupLine("[green]✓ NeuralCore esta completamente configurado.[/]");
            AnsiConsole.MarkupLine("[dim]Se iniciara automaticamente cuando abras tu agente AI.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠ NeuralCore necesita configuracion adicional.[/]");
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static int RunTest()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Test de conectividad NeuralCore[/]");
        AnsiConsole.WriteLine();

        if (!NeuralCoreInstallerService.IsPublished())
        {
            AnsiConsole.MarkupLine("[red]NeuralCore no esta publicado.[/]");
            AnsiConsole.MarkupLine("[dim]Ejecutá: temper-ai neuralcore --publish[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[dim]Iniciando NeuralCore en modo test...[/]");

        var startInfo = new ProcessStartInfo
        {
            FileName = NeuralCoreInstallerService.GetNeuralCoreExePath(),
            Arguments = "--test-ping",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            AnsiConsole.MarkupLine("[red]No se pudo iniciar NeuralCore.[/]");
            return 1;
        }

        bool completed = process.WaitForExit(10000);

        if (!completed)
        {
            process.Kill(true);
            AnsiConsole.MarkupLine("[yellow]⚠ NeuralCore no respondio en 10 segundos.[/]");
            AnsiConsole.MarkupLine("[dim]Puede que este funcionando correctamente (los servidores MCP son de larga duracion).[/]");
        }
        else
        {
            int exitCode = process.ExitCode;

            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]✓[/] [bold]NeuralCore respondio correctamente.[/]");
                string output = process.StandardOutput.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    AnsiConsole.MarkupLine($"[dim]{output.Trim()}[/]");
                }
            }
            else
            {
                string error = process.StandardError.ReadToEnd();
                AnsiConsole.MarkupLine("[red]✗ NeuralCore fallo al iniciar.[/]");
                AnsiConsole.MarkupLine($"[red]{error}[/]");
                return 1;
            }
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static int ShowLogs()
    {
        PrintHeader();

        string neuralCorePath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string logFile = Path.Combine(neuralCorePath, "logs", "neuralcore.log");

        if (!File.Exists(logFile))
        {
            AnsiConsole.MarkupLine("[dim]No se encontraron logs de NeuralCore.[/]");
            AnsiConsole.MarkupLine($"[dim]Path esperado: {logFile}[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine("[bold]Ultimos logs de NeuralCore:[/]");
        AnsiConsole.WriteLine();

        string[] lines = File.ReadAllLines(logFile);
        int start = Math.Max(0, lines.Length - 50);

        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]{line}[/]");
            }
            else if (line.Contains("WARN", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[yellow]{line}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]{line}[/]");
            }
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static void ShowMenu()
    {
        PrintHeader();

        AnsiConsole.MarkupLine("[bold]¿Que queres hacer con NeuralCore?[/]");
        AnsiConsole.WriteLine();

        List<MenuOption> options =
        [
            new("status", "🔍  Verificar estado de NeuralCore y configuracion MCP", "status"),
            new("test", "🧪  Ejecutar test de conectividad", "test"),
            new("publish", "📦  Publicar NeuralCore como ejecutable", "publish"),
            new("install", "⚙️   Instalar configuracion MCP en agentes AI", "install"),
            new("logs", "📜  Ver logs del servidor", "logs"),
        ];

        List<string> displayNames = options.Select(o => o.DisplayName).ToList();

        string? selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Selecciona una opcion:[/]")
                .PageSize(6)
                .AddChoices(displayNames));

        if (string.IsNullOrEmpty(selection))
        {
            return;
        }

        MenuOption? selected = options.FirstOrDefault(o => o.DisplayName == selection);

        if (selected is null)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Ejecutando: temper-ai neuralcore --{selected.CommandName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        CommandApp subApp = new();
        subApp.Configure(config =>
        {
            config.SetApplicationName("temper-ai");
            config.PropagateExceptions();
            config.AddCommand<NeuralCoreCommand>("neuralcore")
                  .WithDescription("Gestiona NeuralCore MCP server");
        });

        subApp.Run(["neuralcore", $"--{selected.CommandName}"]);
    }

    private static List<AgentTarget> ResolveTargets(NeuralCoreSettings settings)
    {
        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.Supported();

        if (!string.IsNullOrEmpty(settings.AgentId))
        {
            AgentTarget? target = AgentTargets.FindById(settings.AgentId);

            if (target is null)
            {
                AnsiConsole.MarkupLine($"[red]Agente no encontrado: {settings.AgentId}[/]");
                return [];
            }

            return [target];
        }

        List<string> choices = supportedTargets.Select(t => t.Name).ToList();
        choices.Add("Todos");

        string selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]¿Para que agente queres configurar NeuralCore?[/]")
                .AddChoices(choices));

        if (selection == "Todos")
        {
            return supportedTargets.ToList();
        }

        AgentTarget? selected = supportedTargets.FirstOrDefault(t => t.Name == selection);

        return selected is not null ? [selected] : [];
    }

    private static bool IsNeuralCoreInConfig(string configPath)
    {
        try
        {
            string content = File.ReadAllText(configPath);
            return content.Contains("neuralcore", StringComparison.OrdinalIgnoreCase)
                || content.Contains("temperai-neuralcore", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("NeuralCore")
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[dim]Memoria persistente MCP para TemperAI[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    private sealed class MenuOption
    {
        public string CommandName { get; }
        public string Description { get; }
        public string DisplayName { get; }

        public MenuOption(string commandName, string description, string icon)
        {
            CommandName = commandName;
            Description = description;
            DisplayName = $"{icon}  {commandName}  —  {description}";
        }
    }
}
