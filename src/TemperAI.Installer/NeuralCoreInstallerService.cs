using System.Diagnostics;
using System.Text.Json;
using TemperAI.Core.Models;

namespace TemperAI.Installer;

public sealed class NeuralCoreInstallerService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InstallResult InstallNeuralCore(AgentTarget target, string neuralCoreExePath)
    {
        List<string> installed = [];
        List<string> skipped = [];
        List<string> errors = [];

        try
        {
            if (target.McpConfigFormat.Equals("claude", StringComparison.OrdinalIgnoreCase))
            {
                return ConfigureClaudeMcp(target, installed, skipped, errors);
            }

            string mcpConfigFile = target.McpConfigFile;

            if (string.IsNullOrEmpty(mcpConfigFile))
            {
                errors.Add($"MCP config path not configured for {target.Name}");
                return new InstallResult
                {
                    Target = target,
                    Installed = installed,
                    Skipped = skipped,
                    Errors = errors,
                    IsSuccess = false
                };
            }

            string? configDirectory = Path.GetDirectoryName(mcpConfigFile);

            if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            bool configExists = File.Exists(mcpConfigFile);

            if (configExists)
            {
                bool alreadyConfigured = IsNeuralCoreConfigured(mcpConfigFile, target.McpConfigFormat);

                if (alreadyConfigured)
                {
                    skipped.Add($"{mcpConfigFile} (NeuralCore already configured)");
                    return new InstallResult
                    {
                        Target = target,
                        Installed = installed,
                        Skipped = skipped,
                        Errors = errors,
                        IsSuccess = true
                    };
                }
            }

            AddNeuralCoreToConfig(mcpConfigFile, target.McpConfigFormat, neuralCoreExePath);
            installed.Add(mcpConfigFile);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to configure NeuralCore for {target.Name}: {ex.Message}");
        }

        return new InstallResult
        {
            Target = target,
            Installed = installed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    private InstallResult ConfigureClaudeMcp(
        AgentTarget target,
        List<string> installed,
        List<string> skipped,
        List<string> errors)
    {
        if (!IsClaudeCliAvailable())
        {
            errors.Add(
                "Claude CLI no encontrado en el PATH. Configurá NeuralCore manualmente: " +
                "claude mcp add neuralcore --scope user -- temper-ai --mcp");

            return BuildResult(target, installed, skipped, errors);
        }

        if (IsClaudeMcpConfigured())
        {
            skipped.Add("Claude MCP (neuralcore ya configurado)");
            return BuildResult(target, installed, skipped, errors);
        }

        ClaudeCliResult result = RunClaudeCli("mcp add neuralcore --scope user -- temper-ai --mcp");

        if (result.Success)
        {
            installed.Add("Claude MCP (neuralcore --scope user)");
        }
        else
        {
            errors.Add($"No se pudo configurar NeuralCore en Claude: {result.Error}");
        }

        return BuildResult(target, installed, skipped, errors);
    }

    private static InstallResult BuildResult(
        AgentTarget target,
        List<string> installed,
        List<string> skipped,
        List<string> errors)
    {
        return new InstallResult
        {
            Target = target,
            Installed = installed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    public static bool IsClaudeCliAvailable()
    {
        try
        {
            ClaudeCliResult result = RunClaudeCli("--version");
            return result.Started;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsClaudeMcpConfigured()
    {
        try
        {
            ClaudeCliResult result = RunClaudeCli("mcp get neuralcore");
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public static ClaudeCliResult RunClaudeCli(string arguments)
    {
        // The Claude CLI is typically a .cmd shim on Windows, so it must be launched
        // through cmd.exe for PATH resolution; elsewhere it can be invoked directly.
        ProcessStartInfo startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c claude {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
            : new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

        try
        {
            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                return new ClaudeCliResult { Started = false };
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ClaudeCliResult
            {
                Started = true,
                ExitCode = process.ExitCode,
                Output = output,
                Error = string.IsNullOrWhiteSpace(error) ? output : error
            };
        }
        catch
        {
            return new ClaudeCliResult { Started = false };
        }
    }

    public static PublishResult PublishNeuralCore(Action<string>? onProgress = null)
    {
        string publishPath = GetNeuralCoreInstallPath();

        if (!Directory.Exists(publishPath))
        {
            Directory.CreateDirectory(publishPath);
        }

        string? projectPath = FindNeuralCoreProjectPath();

        if (string.IsNullOrEmpty(projectPath))
        {
            return new PublishResult
            {
                Success = false,
                Error = "No se encontro el proyecto TemperAI.NeuralCore. Asegurate de ejecutar desde el repositorio."
            };
        }

        onProgress?.Invoke($"Proyecto: {projectPath}");
        onProgress?.Invoke($"Destino:  {publishPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{projectPath}\" -c Release -o \"{publishPath}\" --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return new PublishResult
            {
                Success = false,
                Error = "No se pudo iniciar el proceso de publicacion."
            };
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return new PublishResult
            {
                Success = false,
                Error = error
            };
        }

        string exePath = Path.Combine(publishPath, "TemperAI.NeuralCore.exe");

        if (!File.Exists(exePath))
        {
            return new PublishResult
            {
                Success = false,
                Error = "Publicacion completada pero no se encontro el ejecutable."
            };
        }

        long fileSize = new FileInfo(exePath).Length;
        string sizeStr = fileSize > 1_000_000
            ? $"{fileSize / 1_000_000} MB"
            : $"{fileSize / 1_000} KB";

        return new PublishResult
        {
            Success = true,
            ExePath = exePath,
            Size = sizeStr,
            Output = output
        };
    }

    public static string GetNeuralCoreInstallPath()
    {
        string localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(localAppData, "Programs", "TemperAI", "NeuralCore");
    }

    public static string GetNeuralCoreExePath()
    {
        return Path.Combine(GetNeuralCoreInstallPath(), "TemperAI.NeuralCore.exe");
    }

    public static bool IsPublished()
    {
        return File.Exists(GetNeuralCoreExePath());
    }

    public static string? FindNeuralCoreProjectPath()
    {
        string currentDir = Directory.GetCurrentDirectory();

        for (int i = 0; i < 5; i++)
        {
            string candidate = Path.Combine(currentDir, "src", "TemperAI.NeuralCore", "TemperAI.NeuralCore.csproj");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            string? parent = Directory.GetParent(currentDir)?.FullName;

            if (parent is null)
            {
                break;
            }

            currentDir = parent;
        }

        return null;
    }

    private static bool IsNeuralCoreConfigured(string configPath, string format)
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

    private static void AddNeuralCoreToConfig(string configPath, string format, string neuralCoreExePath)
    {
        JsonDocument? existingConfig = null;
        JsonElement rootElement = default;

        if (File.Exists(configPath))
        {
            try
            {
                string content = File.ReadAllText(configPath);
                existingConfig = JsonDocument.Parse(content);
                rootElement = existingConfig.RootElement.Clone();
            }
            catch
            {
                // If we can't parse, we'll create a fresh config
            }
        }

        var mcpConfig = new Dictionary<string, object>();

        if (rootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in rootElement.EnumerateObject())
            {
                mcpConfig[property.Name] = JsonElementToObject(property.Value);
            }
        }

        var neuralCoreServer = new Dictionary<string, object>
        {
            ["type"] = "local",
            ["command"] = new List<string> { "temper-ai", "--mcp" },
            ["enabled"] = true
        };

        // OpenCode uses structure: { "mcp": { "neuralcore": {...} } }
        // No "servers" level. type: "local", command as array.
        var mcpObj = mcpConfig.ContainsKey("mcp")
            ? (Dictionary<string, object>)mcpConfig["mcp"]
            : new Dictionary<string, object>();

        mcpObj["neuralcore"] = neuralCoreServer;
        mcpConfig["mcp"] = mcpObj;

        string jsonOutput = JsonSerializer.Serialize(mcpConfig, _jsonOptions);
        File.WriteAllText(configPath, jsonOutput);

        existingConfig?.Dispose();
    }

    private static object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(JsonElementToObject).ToList(),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => string.Empty
        };
    }
}

public sealed class PublishResult
{
    public bool Success { get; init; }
    public string? ExePath { get; init; }
    public string? Size { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
}

public sealed class ClaudeCliResult
{
    public bool Started { get; init; }
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool Success => Started && ExitCode == 0;
}
