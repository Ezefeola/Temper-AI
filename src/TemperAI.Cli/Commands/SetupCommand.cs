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
        string currentExe = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

        // Resolvemos paths completos para comparar con precisión
        string fullCurrentExe = Path.GetFullPath(currentExe);
        string fullTargetExe = Path.GetFullPath(targetExe);

        bool isSameFile = string.Equals(fullCurrentExe, fullTargetExe, StringComparison.OrdinalIgnoreCase);

        AnsiConsole.MarkupLine($"[bold]Instalando TemperAI CLI...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Destino: {installDir}[/]");

        try
        {
            Directory.CreateDirectory(installDir);

            if (isSameFile)
            {
                // Ya estamos ejecutando desde la ubicación de instalación.
                // No necesitamos copiar, solo asegurar configs.
                AnsiConsole.MarkupLine("[dim]El ejecutable ya está en el directorio de instalación.[/]");
            }
            else
            {
                // Estamos ejecutando desde otra ubicación (ej. dotnet run).
                // Intentamos copiar directamente.
                try
                {
                    AnsiConsole.MarkupLine("[dim]Copiando CLI...[/]");
                    File.Copy(fullCurrentExe, fullTargetExe, overwrite: true);
                    AnsiConsole.MarkupLine($"  [green]✓[/] Ejecutable copiado.");
                }
                catch (IOException)
                {
                    // Si falla (archivo bloqueado), usamos el script diferido
                    AnsiConsole.MarkupLine("[dim]Archivo bloqueado, programando actualización...[/]");
                    ScheduleCopyViaScript(fullCurrentExe, fullTargetExe);
                }
            }

            AnsiConsole.MarkupLine("[dim]Agregando al PATH del usuario...[/]");
            AddToPath(installDir);

            AnsiConsole.MarkupLine("[dim]Configurando NeuralCore MCP en agentes AI...[/]");
            ConfigureMcpServers(targetExe);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] [bold]¡Instalación exitosa![/]");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("Reiniciá tu terminal para que los cambios en el PATH surtan efecto.");
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

    private static void ScheduleCopyViaScript(string sourceExe, string targetExe)
    {
        string tempScriptPath = Path.Combine(Path.GetTempPath(), "temper-ai-setup.ps1");

        // Script que espera a que el proceso termine y luego copia
        string scriptContent = $@"
            Start-Sleep -Milliseconds 1000
            try {{
                Copy-Item -Path '{sourceExe}' -Destination '{targetExe}' -Force
                Write-Host 'TemperAI actualizado correctamente.'
            }} catch {{
                Write-Host 'Error al actualizar: $_'
            }}
            Remove-Item -Path '{tempScriptPath}' -Force -ErrorAction SilentlyContinue
        ";

        File.WriteAllText(tempScriptPath, scriptContent);

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{tempScriptPath}\"",
            UseShellExecute = true
        };

        Process.Start(startInfo);
        AnsiConsole.MarkupLine($"  [yellow]⚠[/] Actualización programada (se aplicará en unos segundos).");
    }

    private static void PublishNeuralCore(string installDir)
    {
        // ... (lógica existente) ...
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

        // OpenCode usa type: "local" y command como array — no stdio ni string
        var mcpConfig = new Dictionary<string, object>
        {
            ["type"] = "local",
            ["command"] = new[] { temperExe, "--mcp" },
            ["enabled"] = true
        };

        var config = new Dictionary<string, object>
        {
            ["$schema"] = "https://opencode.ai/config.json",
            ["mcp"] = new Dictionary<string, object>
            {
                ["neuralcore"] = mcpConfig
            }
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

        AnsiConsole.MarkupLine($"  [green]✓[/] OpenCode config updated: [dim]{configPath}[/]");
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
