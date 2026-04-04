using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Skills;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class SkillSettings : CommandSettings
{
    [CommandOption("--create")]
    [Description("Crea un nuevo skill template")]
    public bool Create { get; init; }

    [CommandOption("--install")]
    [Description("Instala un skill desde un directorio fuente")]
    public string? Install { get; init; }

    [CommandOption("--list")]
    [Description("Lista todos los skills instalados")]
    public bool List { get; init; }

    [CommandOption("--name")]
    [Description("Nombre del skill (para --create)")]
    public string? Name { get; init; }

    [CommandOption("--category")]
    [Description("Categoria del skill (para --create)")]
    public string? Category { get; init; }

    [CommandOption("--author")]
    [Description("Autor del skill (para --create)")]
    public string? Author { get; init; }

    [CommandOption("--description")]
    [Description("Descripcion del skill (para --create)")]
    public string? Description { get; init; }
}

public sealed class SkillCommand : Command<SkillSettings>
{
    private const string SkillsDir = "assets/skills";

    public override int Execute(CommandContext context, SkillSettings settings)
    {
        PrintHeader();

        SkillCreatorService service = new();

        if (settings.Create)
        {
            return CreateSkill(service, settings);
        }

        if (!string.IsNullOrEmpty(settings.Install))
        {
            return InstallSkill(service, settings.Install);
        }

        if (settings.List)
        {
            return ListSkills(service);
        }

        ShowHelp();

        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static int CreateSkill(SkillCreatorService service, SkillSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]--name es requerido para crear un skill.[/]");
            AnsiConsole.WriteLine();
            return 1;
        }

        string category = settings.Category ?? "custom";
        string name = settings.Name;
        string author = settings.Author ?? "unknown";
        string description = settings.Description ?? $"Custom skill: {name}";

        SkillMetadata metadata = new()
        {
            Name = name,
            Category = category,
            Author = author,
            Description = description,
            Version = "1.0.0",
            Dependencies = [],
            Tags = ["custom"],
            License = "MIT"
        };

        AnsiConsole.MarkupLine($"[bold]Creando skill template: {category}/{name}[/]");
        AnsiConsole.WriteLine();

        SkillResult result = service.CreateSkill(".", category, name, metadata);

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Skill creado en: [bold]{result.SkillPath}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Archivos creados:[/]");

            foreach (string file in result.CreatedFiles)
            {
                AnsiConsole.MarkupLine($"  [green]+[/] {file}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Edita SKILL.md con tus reglas y patrones.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Error al crear skill: {result.Error}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static int InstallSkill(SkillCreatorService service, string sourcePath)
    {
        AnsiConsole.MarkupLine($"[bold]Instalando skill desde: {sourcePath}[/]");
        AnsiConsole.WriteLine();

        SkillResult result = service.InstallSkill(SkillsDir, sourcePath);

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Skill instalado en: [bold]{result.SkillPath}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Archivos instalados:[/]");

            foreach (string file in result.CreatedFiles)
            {
                AnsiConsole.MarkupLine($"  [green]+[/] {file}");
            }

            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Error al instalar skill: {result.Error}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static int ListSkills(SkillCreatorService service)
    {
        AnsiConsole.MarkupLine("[bold]Skills instalados:[/]");
        AnsiConsole.WriteLine();

        IReadOnlyList<SkillInfo> skills = service.DiscoverSkills(SkillsDir);

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No hay skills instalados.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Nombre[/]"));
        table.AddColumn(new TableColumn("[bold]Categoria[/]"));
        table.AddColumn(new TableColumn("[bold]Version[/]"));
        table.AddColumn(new TableColumn("[bold]Autor[/]"));
        table.AddColumn(new TableColumn("[bold]Archivos[/]"));
        table.AddColumn(new TableColumn("[bold]Tags[/]"));

        foreach (SkillInfo skill in skills)
        {
            string tags = skill.Tags.Length > 0 ? string.Join(", ", skill.Tags) : "—";

            table.AddRow(
                skill.Name,
                skill.Category,
                skill.Version,
                skill.Author,
                skill.FileCount.ToString(),
                tags);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        return 0;
    }

    private static void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]Skill Marketplace[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Crea, instala y descubre skills personalizados para tu equipo.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Comandos:[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai skill --create --name my-skill --category backend  Crear skill template[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai skill --install /path/to/skill                    Instalar skill externo[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai skill --list                                        Listar skills instalados[/]");
        AnsiConsole.WriteLine();
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Skill Marketplace — Crea, comparte, descubre[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
