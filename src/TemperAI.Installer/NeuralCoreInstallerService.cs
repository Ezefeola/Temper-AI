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
            ["command"] = new List<string> { "dotnet", "run", "--project", neuralCoreExePath },
            ["enabled"] = true
        };

        switch (format)
        {
            case "opencode":
                // OpenCode uses structure: { "mcp": { "neuralcore": {...} } }
                // No "servers" level. type: "local", command as array.
                var mcpObj = mcpConfig.ContainsKey("mcp")
                    ? (Dictionary<string, object>)mcpConfig["mcp"]
                    : new Dictionary<string, object>();
                
                mcpObj["neuralcore"] = neuralCoreServer;
                mcpConfig["mcp"] = mcpObj;
                break;

            case "claude":
                var claudeMcpServers = mcpConfig.ContainsKey("mcpServers")
                    ? (Dictionary<string, object>)mcpConfig["mcpServers"]
                    : new Dictionary<string, object>();
                claudeMcpServers["neuralcore"] = neuralCoreServer;
                mcpConfig["mcpServers"] = claudeMcpServers;
                break;

            case "copilot":
                var copilotMcp = mcpConfig.ContainsKey("mcp")
                    ? (Dictionary<string, object>)mcpConfig["mcp"]
                    : new Dictionary<string, object>();
                var copilotServers = copilotMcp.ContainsKey("servers")
                    ? (Dictionary<string, object>)copilotMcp["servers"]
                    : new Dictionary<string, object>();
                copilotServers["neuralcore"] = neuralCoreServer;
                copilotMcp["servers"] = copilotServers;
                mcpConfig["mcp"] = copilotMcp;
                break;
        }

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
