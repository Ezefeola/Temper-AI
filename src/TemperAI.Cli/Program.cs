using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Cli.Commands;

if (args.Length == 1 && args[0] == "neuralcore")
{
    var neuralCoreDir = Path.GetDirectoryName(Environment.ProcessPath);
    var neuralCoreExe = Path.Combine(neuralCoreDir, "TemperAI.NeuralCore.exe");

    if (!File.Exists(neuralCoreExe))
    {
        Console.Error.WriteLine("NeuralCore MCP server not found. Run 'temper-ai setup' to install.");
        return 1;
    }

    var startInfo = new ProcessStartInfo
    {
        FileName = neuralCoreExe,
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();
    return process?.ExitCode ?? 0;
}

if (args.Length == 0)
{
    return RunMenu();
}

CommandApp app = new();

app.Configure(config =>
{
    config.SetApplicationName("temper-ai");
    config.PropagateExceptions();

    config.AddCommand<InstallCommand>("install")
          .WithDescription("Instala skills y agentes en tu agente AI")
          .WithExample("install")
          .WithExample("install", "--dry-run")
          .WithExample("install", "--agent", "copilot");

    config.AddCommand<UpdateCommand>("update")
          .WithDescription("Actualiza skills y agentes instalados")
          .WithExample("update")
          .WithExample("update", "--force")
          .WithExample("update", "--dry-run")
          .WithExample("update", "--agent", "copilot");

    config.AddCommand<StatusCommand>("status")
          .WithDescription("Muestra el estado de la instalacion actual")
          .WithExample("status");

    config.AddCommand<BudgetCommand>("budget")
          .WithDescription("Muestra el uso de tokens del proyecto")
          .WithExample("budget")
          .WithExample("budget", "--reset");

    config.AddCommand<SnapshotCommand>("snapshot")
          .WithDescription("Gestiona snapshots para rollback automatico")
          .WithExample("snapshot")
          .WithExample("snapshot", "--create", "--phase", "init")
          .WithExample("snapshot", "--latest")
          .WithExample("snapshot", "--restore", "20260404-120000_init")
          .WithExample("snapshot", "--delete", "20260404-120000_init");

    config.AddCommand<IncrementalCommand>("incremental")
          .WithDescription("Detecta que fases necesitan re-ejecutarse tras un cambio")
          .WithExample("incremental")
          .WithExample("incremental", "--check")
          .WithExample("incremental", "--force");

    config.AddCommand<SkillCommand>("skill")
          .WithDescription("Crea, instala y descubre skills personalizados")
          .WithExample("skill", "--create", "--name", "my-skill", "--category", "backend")
          .WithExample("skill", "--install", "/path/to/skill")
          .WithExample("skill", "--list");

    config.AddCommand<MenuCommand>("menu")
          .WithDescription("Menu interactivo con todos los comandos")
          .WithExample("menu");

    config.AddCommand<SetupCommand>("setup")
          .WithDescription("Instala temper-ai.exe en el PATH global")
          .WithExample("setup");

    config.AddCommand<NeuralCommand>("neural")
          .WithDescription("Guarda y recupera observaciones del proyecto (NeuralCore)")
          .WithExample("neural", "--save", "--title", "Fix null ref", "--content", "Fixed...", "--type", "Bugfix")
          .WithExample("neural", "--recall", "--limit", "20")
          .WithExample("neural", "--recall", "--topic-filter", "null-ref-fix")
          .WithExample("neural", "--session", "--project", "MyProject");
});

return app.Run(args);

static int RunMenu()
{
    AnsiConsole.WriteLine();
    AnsiConsole.Write(
        new FigletText("TemperAI")
            .Color(Color.Purple));

    AnsiConsole.MarkupLine("[dim]Ecosistema AI para desarrolladores .NET[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
    AnsiConsole.WriteLine();

    List<CommandOption> commands =
    [
        new("install", "Instala skills y agentes en tu agente AI", "install"),
        new("update", "Actualiza skills y agentes instalados", "update"),
        new("status", "Muestra el estado de la instalacion actual", "status"),
        new("budget", "Muestra el uso de tokens del proyecto", "budget"),
        new("snapshot", "Gestiona snapshots para rollback automatico", "snapshot"),
        new("incremental", "Detecta que fases necesitan re-ejecutarse", "incremental"),
        new("skill", "Crea, instala y descubre skills personalizados", "skill"),
        new("setup", "Instala temper-ai.exe en el PATH global", "setup")
    ];

    List<string> displayNames = commands.Select(c => c.DisplayName).ToList();

    string? selection = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]¿Que queres hacer?[/] [dim](escribi para filtrar)[/]")
            .PageSize(10)
            .AddChoices(displayNames));

    if (string.IsNullOrEmpty(selection))
    {
        AnsiConsole.WriteLine();
        return 0;
    }

    CommandOption? selected = commands.FirstOrDefault(c => c.DisplayName == selection);

    if (selected is null)
    {
        return 0;
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold]Comando:[/] temper-ai {selected.CommandName}");
    AnsiConsole.MarkupLine($"[bold]Descripcion:[/] {selected.Description}");
    AnsiConsole.WriteLine();

    bool runCommand = AnsiConsole.Confirm("¿Queres ejecutar este comando?");

    if (!runCommand)
    {
        AnsiConsole.MarkupLine("[dim]Cancelado. Volviendo al menu...[/]");
        AnsiConsole.WriteLine();
        return RunMenu();
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[dim]Ejecutando: temper-ai {selected.CommandName}[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
    AnsiConsole.WriteLine();

    return RunSubCommand(selected.CommandName);
}

static int RunSubCommand(string commandName)
{
    string exePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

    var startInfo = new ProcessStartInfo
    {
        FileName = exePath,
        Arguments = commandName,
        UseShellExecute = false
    };

    using var process = Process.Start(startInfo);
    process?.WaitForExit();
    return process?.ExitCode ?? 0;
}

sealed class CommandOption
{
    public string CommandName { get; }
    public string Description { get; }
    public string DisplayName { get; }

    public CommandOption(string commandName, string description, string icon)
    {
        CommandName = commandName;
        Description = description;
        DisplayName = $"{icon}  {commandName}  —  {description}";
    }
}
