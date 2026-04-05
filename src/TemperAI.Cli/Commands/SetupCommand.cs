using Microsoft.Win32;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

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

            AnsiConsole.MarkupLine($"[dim]Copiando CLI a: {installDir}[/]");
            File.Copy(currentExe, targetExe, overwrite: true);

            AnsiConsole.MarkupLine("[dim]Publicando NeuralCore MCP server...[/]");
            PublishNeuralCore(installDir);

            AnsiConsole.MarkupLine("[dim]Agregando al PATH del usuario...[/]");
            AddToPath(installDir);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Configurando NeuralCore MCP en agentes AI...[/]");
            ConfigureMcpServers(targetExe);

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

    private static void PublishNeuralCore(string installDir)
    {
        string solutionDir = FindSolutionDirectory();

        if (string.IsNullOrEmpty(solutionDir))
        {
            AnsiConsole.MarkupLine("  [yellow]⚠[/] No se encontró la solución. NeuralCore no fue publicado.");
            AnsiConsole.MarkupLine("  [dim]Ejecutá esto manualmente: dotnet publish src/TemperAI.NeuralCore -c Release -o [installDir][/]");
            return;
        }

        string projectPath = Path.Combine(solutionDir, "src", "TemperAI.NeuralCore", "TemperAI.NeuralCore.csproj");

        if (!File.Exists(projectPath))
        {
            AnsiConsole.MarkupLine("  [yellow]⚠[/] No se encontró el proyecto NeuralCore.");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c Release -o \"{installDir}\" --no-self-contained",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        process?.WaitForExit();

        if (process?.ExitCode == 0)
        {
            AnsiConsole.MarkupLine($"  [green]✓[/] NeuralCore publicado en: [dim]{installDir}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("  [yellow]⚠[/] Error al publicar NeuralCore. Verificá que .NET 10 SDK esté instalado.");
        }
    }

    private static string? FindSolutionDirectory()
    {
        string? currentDir = AppDomain.CurrentDomain.BaseDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            if (Directory.GetFiles(currentDir, "*.slnx").Length > 0 ||
                Directory.GetFiles(currentDir, "*.sln").Length > 0)
            {
                return currentDir;
            }

            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
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

    private static void ConfigureMcpServers(string temperExe)
    {
        ConfigureOpenCode(temperExe);
        ConfigureCopilot(temperExe);
    }

    private static void ConfigureOpenCode(string temperExe)
    {
        string configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "opencode");

        Directory.CreateDirectory(configDir);

        string configPath = Path.Combine(configDir, "opencode.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var mcpConfig = new Dictionary<string, object>
        {
            ["type"] = "stdio",
            ["command"] = temperExe,
            ["args"] = new[] { "neuralcore" }
        };

        var config = new Dictionary<string, object>
        {
            ["mcp"] = new Dictionary<string, object>
            {
                ["neuralcore"] = mcpConfig
            }
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

        AnsiConsole.MarkupLine($"  [green]✓[/] OpenCode config updated: [dim]{configPath}[/]");
    }

    private static void ConfigureCopilot(string temperExe)
    {
        string configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".copilot");

        Directory.CreateDirectory(configDir);

        string configPath = Path.Combine(configDir, "mcp.json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var mcpConfig = new Dictionary<string, object>
        {
            ["type"] = "stdio",
            ["command"] = temperExe,
            ["args"] = new[] { "neuralcore" }
        };

        var config = new Dictionary<string, object>
        {
            ["mcpServers"] = new Dictionary<string, object>
            {
                ["neuralcore"] = mcpConfig
            }
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

        AnsiConsole.MarkupLine($"  [green]✓[/] Copilot CLI config updated: [dim]{configPath}[/]");
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
