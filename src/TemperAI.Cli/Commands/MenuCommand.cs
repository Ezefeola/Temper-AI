using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class MenuSettings : CommandSettings
{
}

public sealed class MenuCommand : Command<MenuSettings>
{
    public override int Execute(CommandContext context, MenuSettings settings)
    {
        PrintHeader();

        List<CommandOption> commands =
        [
            new("install", "Instala skills y agentes en OpenCode", ["--dry-run", "--source local"]),
            new("update", "Actualiza CLI y assets instalados", ["--force", "--dry-run", "--source local"]),
            new("status", "Muestra el estado de la instalacion actual", []),
            new("neuralcore", "Gestiona NeuralCore MCP server (memoria persistente)", []),
            new("budget", "Muestra el uso de tokens del proyecto", ["--reset"]),
            new("snapshot", "Gestiona snapshots para rollback automatico", ["--create --phase init", "--latest", "--restore"]),
            new("incremental", "Detecta que fases necesitan re-ejecutarse", ["--check", "--force"]),
            new("skill", "Crea, instala y descubre skills personalizados", ["--create --name my-skill", "--list"])
        ];

        List<string> displayNames = commands.Select(c => c.DisplayName).ToList();

        string? selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]¿Que queres hacer?[/] [dim](usa las flechas o escribi para buscar)[/]")
                .PageSize(10)
                .AddChoices(displayNames));

        if (string.IsNullOrEmpty(selection))
        {
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

        if (selected.Examples.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Ejemplos:[/]");

            foreach (string example in selected.Examples)
            {
                AnsiConsole.MarkupLine($"  [dim]temper-ai {selected.CommandName} {example}[/]");
            }
        }

        AnsiConsole.WriteLine();

        bool runCommand = AnsiConsole.Confirm("¿Queres ejecutar este comando?");

        if (!runCommand)
        {
            AnsiConsole.MarkupLine("[dim]Cancelado.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        string args = $"temper-ai {selected.CommandName}";

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Ejecutando: {args}[/]");
        AnsiConsole.WriteLine();

        return RunSubCommand(selected.CommandName);
    }

    private static int RunSubCommand(string commandName)
    {
        CommandApp subApp = new();

        subApp.Configure(config =>
        {
            config.SetApplicationName("temper-ai");
            config.PropagateExceptions();

            config.AddCommand<InstallCommand>("install")
                  .WithDescription("Instala skills y agentes en OpenCode");

            config.AddCommand<UpdateCommand>("update")
                  .WithDescription("Actualiza CLI y assets instalados");

            config.AddCommand<StatusCommand>("status")
                  .WithDescription("Muestra el estado de la instalacion actual");

            config.AddCommand<BudgetCommand>("budget")
                  .WithDescription("Muestra el uso de tokens del proyecto");

            config.AddCommand<SnapshotCommand>("snapshot")
                  .WithDescription("Gestiona snapshots para rollback automatico");

            config.AddCommand<IncrementalCommand>("incremental")
                  .WithDescription("Detecta que fases necesitan re-ejecutarse tras un cambio");

            config.AddCommand<SkillCommand>("skill")
                  .WithDescription("Crea, instala y descubre skills personalizados");

            config.AddCommand<NeuralCoreCommand>("neuralcore")
                  .WithDescription("Gestiona NeuralCore MCP server (status, test, install, update)");
        });

        return subApp.Run([commandName]);
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Menu interactivo — selecciona un comando[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    private sealed class CommandOption
    {
        public string CommandName { get; }
        public string Description { get; }
        public List<string> Examples { get; }
        public string DisplayName { get; }

        public CommandOption(string commandName, string description, List<string> examples)
        {
            CommandName = commandName;
            Description = description;
            Examples = examples;
            DisplayName = $"{commandName} — {description}";
        }
    }
}
