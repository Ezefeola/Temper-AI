using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Snapshots;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class SnapshotSettings : CommandSettings
{
    [CommandOption("--create")]
    [Description("Crea un snapshot del estado actual")]
    public bool Create { get; init; }

    [CommandOption("--restore")]
    [Description("Restaura un snapshot por nombre")]
    public string? Restore { get; init; }

    [CommandOption("--latest")]
    [Description("Restaura el ultimo snapshot disponible")]
    public bool Latest { get; init; }

    [CommandOption("--delete")]
    [Description("Elimina un snapshot por nombre")]
    public string? Delete { get; init; }

    [CommandOption("--phase")]
    [Description("Nombre de la fase para el snapshot (solo con --create)")]
    public string? Phase { get; init; }
}

public sealed class SnapshotCommand : Command<SnapshotSettings>
{
    private const string TemperDir = ".temper";

    public override int Execute(CommandContext context, SnapshotSettings settings)
    {
        PrintHeader();

        SnapshotService snapshotService = new();

        if (settings.Create)
        {
            return CreateSnapshot(snapshotService, settings.Phase ?? "manual");
        }

        if (!string.IsNullOrEmpty(settings.Restore))
        {
            return RestoreSnapshot(snapshotService, settings.Restore);
        }

        if (settings.Latest)
        {
            return RestoreLatestSnapshot(snapshotService);
        }

        if (!string.IsNullOrEmpty(settings.Delete))
        {
            return DeleteSnapshot(snapshotService, settings.Delete);
        }

        ListSnapshots(snapshotService);

        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static int CreateSnapshot(SnapshotService service, string phaseName)
    {
        if (!Directory.Exists(TemperDir))
        {
            AnsiConsole.MarkupLine("[red]Directorio .temper/ no encontrado. Ejecutá temper-ai init primero.[/]");
            AnsiConsole.WriteLine();
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold]Creando snapshot de fase: {phaseName}[/]");
        AnsiConsole.WriteLine();

        SnapshotResult result = service.CreateSnapshot(TemperDir, phaseName);

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Snapshot creado: [bold]{result.SnapshotPath}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Archivos incluidos:[/]");

            foreach (string file in result.Files)
            {
                AnsiConsole.MarkupLine($"  [green]+[/] {file}");
            }

            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Error al crear snapshot: {result.Error}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static int RestoreSnapshot(SnapshotService service, string snapshotName)
    {
        if (!Directory.Exists(TemperDir))
        {
            AnsiConsole.MarkupLine("[red]Directorio .temper/ no encontrado.[/]");
            AnsiConsole.WriteLine();
            return 1;
        }

        bool confirmed = AnsiConsole.Confirm(
            $"¿Seguro que queres restaurar el snapshot [bold]{snapshotName}[/]? Se sobreescribiran los archivos actuales.");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[dim]Restauracion cancelada.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[bold]Restaurando snapshot: {snapshotName}[/]");
        AnsiConsole.WriteLine();

        SnapshotResult result = service.RestoreSnapshot(TemperDir, snapshotName);

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Snapshot restaurado: [bold]{snapshotName}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Archivos restaurados:[/]");

            foreach (string file in result.Files)
            {
                AnsiConsole.MarkupLine($"  [green]↺[/] {file}");
            }

            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Error al restaurar: {result.Error}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static int RestoreLatestSnapshot(SnapshotService service)
    {
        if (!Directory.Exists(TemperDir))
        {
            AnsiConsole.MarkupLine("[red]Directorio .temper/ no encontrado.[/]");
            AnsiConsole.WriteLine();
            return 1;
        }

        SnapshotInfo? latest = service.GetLatestSnapshot(TemperDir);

        if (latest is null)
        {
            AnsiConsole.MarkupLine("[yellow]No hay snapshots disponibles.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        bool confirmed = AnsiConsole.Confirm(
            $"¿Seguro que queres restaurar el ultimo snapshot [bold]{latest.Name}[/] (fase: {latest.Phase})?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[dim]Restauracion cancelada.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        SnapshotResult result = service.RestoreSnapshot(TemperDir, latest.Name);

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Ultimo snapshot restaurado: [bold]{latest.Name}[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Error al restaurar: {result.Error}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static int DeleteSnapshot(SnapshotService service, string snapshotName)
    {
        if (!Directory.Exists(TemperDir))
        {
            AnsiConsole.MarkupLine("[red]Directorio .temper/ no encontrado.[/]");
            AnsiConsole.WriteLine();
            return 1;
        }

        bool confirmed = AnsiConsole.Confirm(
            $"¿Seguro que queres eliminar el snapshot [bold]{snapshotName}[/]?");

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[dim]Eliminacion cancelada.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        bool deleted = service.DeleteSnapshot(TemperDir, snapshotName);

        if (deleted)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Snapshot eliminado: [bold]{snapshotName}[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] Snapshot no encontrado: {snapshotName}");
        AnsiConsole.WriteLine();
        return 1;
    }

    private static void ListSnapshots(SnapshotService service)
    {
        if (!Directory.Exists(TemperDir))
        {
            AnsiConsole.MarkupLine("[yellow]Directorio .temper/ no encontrado. Ejecutá temper-ai init primero.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        IReadOnlyList<SnapshotInfo> snapshots = service.ListSnapshots(TemperDir);

        if (snapshots.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No hay snapshots disponibles.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Usá [bold]temper-ai snapshot --create --phase init[/] para crear el primero.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Nombre[/]"));
        table.AddColumn(new TableColumn("[bold]Fase[/]"));
        table.AddColumn(new TableColumn("[bold]Fecha[/]"));
        table.AddColumn(new TableColumn("[bold]Archivos[/]"));
        table.AddColumn(new TableColumn("[bold]Tamaño[/]"));

        foreach (SnapshotInfo snapshot in snapshots)
        {
            string date = snapshot.CreatedAt != DateTime.MinValue
                ? snapshot.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                : "unknown";

            string size = FormatBytes(snapshot.TotalSizeBytes);

            table.AddRow(
                snapshot.Name,
                snapshot.Phase,
                date,
                snapshot.FileCount.ToString(),
                size);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Comandos:[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai snapshot --restore <nombre>  Restaurar un snapshot[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai snapshot --latest             Restaurar el ultimo snapshot[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai snapshot --delete <nombre>    Eliminar un snapshot[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai snapshot --create --phase X   Crear un nuevo snapshot[/]");
        AnsiConsole.WriteLine();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Snapshot Manager — Rollback automatico[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
