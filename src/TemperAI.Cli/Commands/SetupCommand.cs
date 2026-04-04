using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace TemperAI.Cli.Commands;

public sealed class SetupSettings : CommandSettings
{
}

public sealed class SetupCommand : Command<SetupSettings>
{
    public override int Execute(CommandContext context, SetupSettings settings)
    {
        PrintHeader();

        string installDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "TemperAI");

        string targetExe = Path.Combine(installDir, "temper-ai.exe");

        AnsiConsole.MarkupLine($"[bold]Instalando TemperAI CLI...[/]");
        AnsiConsole.WriteLine();

        try
        {
            string currentExe = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

            if (!File.Exists(currentExe))
            {
                AnsiConsole.MarkupLine("[red]No se pudo detectar el ejecutable actual. Asegurate de ejecutar esto desde la app publicada.[/]");
                AnsiConsole.WriteLine();
                return 1;
            }

            Directory.CreateDirectory(installDir);

            AnsiConsole.MarkupLine($"[dim]Copiando ejecutable a: {installDir}[/]");
            File.Copy(currentExe, targetExe, overwrite: true);

            AnsiConsole.MarkupLine("[dim]Agregando al PATH del usuario...[/]");
            AddToPath(installDir);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] [bold]¡Instalación exitosa![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Ahora podés usar [bold]temper-ai[/] desde cualquier terminal.");
            AnsiConsole.MarkupLine("[yellow]Nota: Reiniciá tu terminal para que los cambios en el PATH surtan efecto.[/]");
            AnsiConsole.WriteLine();

            return 0;
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Error durante la instalación: {exception.Message}");
            AnsiConsole.WriteLine();
            return 1;
        }
    }

    private static void AddToPath(string directory)
    {
        string? currentPath = Environment.GetEnvironmentVariable(
            "PATH", EnvironmentVariableTarget.User);

        if (currentPath is null)
        {
            currentPath = string.Empty;
        }

        if (currentPath.Contains(directory, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[dim]El directorio ya está en el PATH.[/]");
            return;
        }

        string newPath = string.IsNullOrEmpty(currentPath)
            ? directory
            : $"{currentPath};{directory}";

        Environment.SetEnvironmentVariable(
            "PATH", newPath, EnvironmentVariableTarget.User);
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Instalador CLI — PATH automático[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
