using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Cli.Services;
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

    [CommandOption("--start")]
    [Description("Inicia el servidor NeuralCore")]
    public bool Start { get; init; }

    [CommandOption("--stop")]
    [Description("Detiene el servidor NeuralCore")]
    public bool Stop { get; init; }

    [CommandOption("--restart")]
    [Description("Reinicia el servidor NeuralCore")]
    public bool Restart { get; init; }

    [CommandOption("--health")]
    [Description("Ejecuta un health check completo de NeuralCore")]
    public bool Health { get; init; }

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

        if (settings.Start)
        {
            return StartNeuralCore();
        }

        if (settings.Stop)
        {
            return StopNeuralCore();
        }

        if (settings.Restart)
        {
            return RestartNeuralCore();
        }

        if (settings.Health)
        {
            return RunHealthCheck();
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

    private static int StartNeuralCore()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Iniciando NeuralCore...[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        NeuralCoreStartResult result = service.Start();

        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] [bold]NeuralCore iniciado correctamente[/]");
            AnsiConsole.MarkupLine($"  [dim]PID:[/] {result.ProcessId}");
            AnsiConsole.MarkupLine($"  [dim]Logs:[/] {result.LogPath}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Presiona [bold]Ctrl+C[/] para detener.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {result.ErrorMessage}");
            AnsiConsole.WriteLine();
            return 1;
        }

        Console.ReadLine();
        return 0;
    }

    private static int StopNeuralCore()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Deteniendo NeuralCore...[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        NeuralCoreStopResult result = service.Stop();

        if (result.Success)
        {
            if (result.KilledProcessId.HasValue)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] [bold]NeuralCore detenido[/]");
                AnsiConsole.MarkupLine($"  [dim]PID detenido:[/] {result.KilledProcessId}");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]NeuralCore no estaba en ejecución.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {result.ErrorMessage}");
            AnsiConsole.WriteLine();
            return 1;
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static int RestartNeuralCore()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Reiniciando NeuralCore...[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        NeuralCoreStartResult result = service.Restart();

        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] [bold]NeuralCore reiniciado correctamente[/]");
            AnsiConsole.MarkupLine($"  [dim]Nuevo PID:[/] {result.ProcessId}");
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {result.ErrorMessage}");
            AnsiConsole.WriteLine();
            return 1;
        }

        return 0;
    }

    private static int RunHealthCheck()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Health Check de NeuralCore[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        NeuralCoreHealthCheckResult result = service.HealthCheck();

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Check[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));

        table.AddRow("Executable existe", result.Exists ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("Puede iniciar", result.CanStart ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("Base de datos accesible", result.DatabaseAccessible ? "[green]✓[/]" : "[red]✗[/]");
        table.AddRow("MCP configurado", result.McpConfigured ? "[green]✓[/]" : "[red]✗[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (result.IsHealthy)
        {
            AnsiConsole.MarkupLine("[green]✓ NeuralCore está saludable[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Se detectaron problemas:[/]");

            foreach (string issue in result.Issues)
            {
                AnsiConsole.MarkupLine($"  [yellow]•[/] {issue}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Recomendaciones:[/]");

            foreach (string rec in result.Recommendations)
            {
                AnsiConsole.MarkupLine($"  [dim]▶[/] {rec}");
            }
        }

        AnsiConsole.WriteLine();
        return result.IsHealthy ? 0 : 1;
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
            AnsiConsole.MarkupLine("[dim]Ahora ejecutá [bold]temper-ai install[/] o [bold]temper-ai neuralcore --install[/]");
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

        var service = new NeuralCoreService();
        NeuralCoreStatus status = service.GetStatus();

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Componente[/]"));
        table.AddColumn(new TableColumn("[bold]Estado[/]"));
        table.AddColumn(new TableColumn("[bold]Detalle[/]"));

        table.AddRow(
            "Executable",
            status.IsPublished ? "[green]Publicado[/]" : "[red]No publicado[/]",
            status.IsPublished ? $"{status.FileSizeDisplay}" : "Ejecutá --publish");

        table.AddRow(
            "Ejecución",
            status.IsRunning ? "[green]Ejecutando[/]" : "[yellow]Detenido[/]",
            status.IsRunning ? $"PID: {status.ProcessId}, {status.RunningDurationDisplay}" : "Ejecutá --start");

        table.AddRow(
            "MCP: OpenCode",
            status.IsConfiguredForOpenCode ? "[green]Configurado[/]" : "[yellow]No configurado[/]",
            status.IsConfiguredForOpenCode ? "Listo" : "Ejecutá --install");

        table.AddRow(
            "MCP: Copilot",
            status.IsConfiguredForCopilot ? "[green]Configurado[/]" : "[yellow]No configurado[/]",
            status.IsConfiguredForCopilot ? "Listo" : "Ejecutá --install");

        table.AddRow(
            "MCP: Claude",
            status.IsConfiguredForClaude ? "[green]Configurado[/]" : "[yellow]No configurado[/]",
            status.IsConfiguredForClaude ? "Listo" : "Ejecutá --install");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (status.SuggestedAction is not null)
        {
            AnsiConsole.MarkupLine($"[dim]💡 {status.SuggestedAction}[/]");
        }

        AnsiConsole.WriteLine();
        return 0;
    }

    private static int RunTest()
    {
        PrintHeader();
        AnsiConsole.MarkupLine("[bold]Test de conectividad NeuralCore[/]");
        AnsiConsole.WriteLine();

        var service = new NeuralCoreService();
        NeuralCoreHealthCheckResult result = service.HealthCheck();

        AnsiConsole.MarkupLine("[dim]Ejecutando test...[/]");
        AnsiConsole.WriteLine();

        if (result.IsHealthy)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] [bold]NeuralCore está funcionando correctamente[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] [bold]NeuralCore tiene problemas:[/]");

            foreach (string issue in result.Issues)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {issue}");
            }
        }

        AnsiConsole.WriteLine();
        return result.IsHealthy ? 0 : 1;
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
            new("status", "Verificar estado de NeuralCore", "🔍"),
            new("health", "Ejecutar health check completo", "🩺"),
            new("start", "Iniciar NeuralCore", "▶️"),
            new("stop", "Detener NeuralCore", "⏹️"),
            new("restart", "Reiniciar NeuralCore", "🔄"),
            new("test", "Probar conectividad con el servidor MCP", "🧪"),
            new("publish", "Publicar NeuralCore como ejecutable standalone", "📦"),
            new("install", "Configurar MCP en agentes AI", "⚙️"),
            new("logs", "Ver logs del servidor MCP", "📜"),
        ];

        List<string> displayNames = options.Select(o => o.DisplayName).ToList();

        string? selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Selecciona una opcion:[/]")
                .PageSize(10)
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