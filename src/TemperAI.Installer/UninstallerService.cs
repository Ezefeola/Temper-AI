using System.Text.Json;
using Microsoft.Win32;
using TemperAI.Core.Assets;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;

namespace TemperAI.Installer;

public sealed class UninstallerService
{
    private readonly bool _dryRun;

    public UninstallerService(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public UninstallResult UninstallAgent(AgentTarget target)
    {
        List<string> removed = [];
        List<string> skipped = [];
        List<string> errors = [];

        UninstallDirectory("skills", target.SkillsPath, removed, skipped, errors);
        UninstallDirectory("agents", target.AgentsPath, removed, skipped, errors);
        RemoveNeuralCoreFromMcpConfig(target.McpConfigFile, target.McpConfigFormat, removed, skipped, errors);

        return new UninstallResult
        {
            Component = $"{target.Name} (skills & agents)",
            Removed = removed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    public UninstallResult UninstallNeuralCore()
    {
        List<string> removed = [];
        List<string> skipped = [];
        List<string> errors = [];

        string neuralCorePath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();

        if (Directory.Exists(neuralCorePath))
        {
            if (_dryRun)
            {
                removed.Add($"{neuralCorePath} (dry run)");
            }
            else
            {
                try
                {
                    Directory.Delete(neuralCorePath, recursive: true);
                    removed.Add(neuralCorePath);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete NeuralCore directory: {ex.Message}");
                }
            }
        }
        else
        {
            skipped.Add($"{neuralCorePath} (not found)");
        }

        return new UninstallResult
        {
            Component = "NeuralCore",
            Removed = removed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    public UninstallResult UninstallCli()
    {
        List<string> removed = [];
        List<string> skipped = [];
        List<string> errors = [];

        string installDir = InstallationPaths.InstallRoot;
        string targetExe = InstallationPaths.CliExePath;

        if (File.Exists(targetExe))
        {
            if (_dryRun)
            {
                removed.Add($"{targetExe} (dry run)");
            }
            else
            {
                try
                {
                    File.Delete(targetExe);
                    removed.Add(targetExe);
                }
                catch (IOException)
                {
                    // File is in use — schedule deletion via script
                    ScheduleDeleteViaScript(targetExe);
                    removed.Add($"{targetExe} (scheduled for deletion)");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete CLI exe: {ex.Message}");
                }
            }
        }
        else
        {
            skipped.Add($"{targetExe} (not found)");
        }

        if (Directory.Exists(InstallationPaths.StateDirectory))
        {
            if (_dryRun)
            {
                removed.Add($"{InstallationPaths.StateDirectory} (dry run)");
            }
            else
            {
                try
                {
                    Directory.Delete(InstallationPaths.StateDirectory, recursive: true);
                    removed.Add(InstallationPaths.StateDirectory);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete state directory: {ex.Message}");
                }
            }
        }

        // Clean up empty TemperAI directory if nothing remains
        if (!_dryRun && Directory.Exists(installDir))
        {
            try
            {
                if (Directory.GetFiles(installDir).Length == 0 &&
                    Directory.GetDirectories(installDir).Length == 0)
                {
                    Directory.Delete(installDir);
                    removed.Add(installDir);
                }
            }
            catch
            {
                // Ignore — directory will be cleaned manually if needed
            }
        }

        return new UninstallResult
        {
            Component = "TemperAI CLI",
            Removed = removed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    public UninstallResult RemoveFromPath()
    {
        List<string> removed = [];
        List<string> skipped = [];
        List<string> errors = [];

        string installDir = InstallationPaths.InstallRoot;

        string? currentPath = Environment.GetEnvironmentVariable(
            "PATH", EnvironmentVariableTarget.User);

        if (string.IsNullOrEmpty(currentPath))
        {
            skipped.Add("PATH (not configured)");
        }
        else if (currentPath.Contains(installDir, StringComparison.OrdinalIgnoreCase))
        {
            if (_dryRun)
            {
                removed.Add($"{installDir} from PATH (dry run)");
            }
            else
            {
                var pathEntries = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var filteredEntries = pathEntries
                    .Where(entry => !entry.Equals(installDir, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                string newPath = string.Join(";", filteredEntries);
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                removed.Add($"{installDir} removed from PATH");
            }
        }
        else
        {
            skipped.Add("PATH (TemperAI not found in PATH)");
        }

        return new UninstallResult
        {
            Component = "PATH",
            Removed = removed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    public List<UninstallResult> UninstallAll(bool includePath = true, bool includeCli = true)
    {
        var results = new List<UninstallResult>();

        // Uninstall from all supported agent targets
        IReadOnlyList<AgentTarget> targets = AgentTargets.Supported();

        foreach (AgentTarget target in targets)
        {
            UninstallResult agentResult = UninstallAgent(target);
            results.Add(agentResult);
        }

        // Uninstall NeuralCore
        UninstallResult neuralCoreResult = UninstallNeuralCore();
        results.Add(neuralCoreResult);

        // Remove from PATH
        if (includePath)
        {
            UninstallResult pathResult = RemoveFromPath();
            results.Add(pathResult);
        }

        // Uninstall CLI (must be last — we're running from it)
        if (includeCli)
        {
            UninstallResult cliResult = UninstallCli();
            results.Add(cliResult);
        }

        return results;
    }

    public List<string> GetPlannedDeletions()
    {
        var deletions = new List<string>();

        // Skills and agents from all targets
        IReadOnlyList<AgentTarget> targets = AgentTargets.Supported();

        foreach (AgentTarget target in targets)
        {
            deletions.AddRange(GetPlannedDirectoryDeletions("skills", target.SkillsPath));
            deletions.AddRange(GetPlannedDirectoryDeletions("agents", target.AgentsPath));

            if (File.Exists(target.McpConfigFile))
            {
                deletions.Add($"[MCP] Remove neuralcore from {target.McpConfigFile}");
            }
        }

        // NeuralCore
        string neuralCorePath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        if (Directory.Exists(neuralCorePath))
        {
            deletions.Add(neuralCorePath);
        }

        // CLI
        string installDir = InstallationPaths.InstallRoot;
        string targetExe = InstallationPaths.CliExePath;
        if (File.Exists(targetExe))
        {
            deletions.Add(targetExe);
        }

        // PATH
        string? currentPath = Environment.GetEnvironmentVariable(
            "PATH", EnvironmentVariableTarget.User);

        if (currentPath?.Contains(installDir, StringComparison.OrdinalIgnoreCase) == true)
        {
            deletions.Add($"{installDir} (from PATH)");
        }

        return deletions;
    }

    private void UninstallDirectory(
        string assetPrefix,
        string destinationDirectory,
        List<string> removed,
        List<string> skipped,
        List<string> errors)
    {
        IReadOnlyList<string> assetPaths = EmbeddedAssets.ListPaths(assetPrefix);

        foreach (string assetPath in assetPaths)
        {
            string fileName = Path.GetFileName(assetPath);

            // Skip README files — same as installer
            if (fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = assetPath[(assetPrefix.Length + 1)..];
            string destinationPath = Path.Combine(destinationDirectory, relativePath);

            if (File.Exists(destinationPath))
            {
                if (_dryRun)
                {
                    removed.Add($"{destinationPath} (dry run)");
                }
                else
                {
                    try
                    {
                        File.Delete(destinationPath);
                        removed.Add(destinationPath);

                        // Clean up empty parent directories
                        CleanupEmptyDirectories(Path.GetDirectoryName(destinationPath), destinationDirectory);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{destinationPath}: {ex.Message}");
                    }
                }
            }
            else
            {
                skipped.Add($"{destinationPath} (not found)");
            }
        }
    }

    private List<string> GetPlannedDirectoryDeletions(string assetPrefix, string destinationDirectory)
    {
        var paths = new List<string>();
        IReadOnlyList<string> assetPaths = EmbeddedAssets.ListPaths(assetPrefix);

        foreach (string assetPath in assetPaths)
        {
            string fileName = Path.GetFileName(assetPath);

            if (fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = assetPath[(assetPrefix.Length + 1)..];
            string destinationPath = Path.Combine(destinationDirectory, relativePath);

            if (File.Exists(destinationPath))
            {
                paths.Add(destinationPath);
            }
        }

        return paths;
    }

    private static void CleanupEmptyDirectories(string? directoryPath, string rootDirectory)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            return;
        }

        // Don't delete the root directory itself
        if (directoryPath.Equals(rootDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (Directory.Exists(directoryPath) &&
                Directory.GetFiles(directoryPath).Length == 0 &&
                Directory.GetDirectories(directoryPath).Length == 0)
            {
                Directory.Delete(directoryPath);
                CleanupEmptyDirectories(Path.GetDirectoryName(directoryPath), rootDirectory);
            }
        }
        catch
        {
            // Ignore — directory might be in use or have permission issues
        }
    }

    private static void RemoveNeuralCoreFromMcpConfig(
        string configPath,
        string format,
        List<string> removed,
        List<string> skipped,
        List<string> errors)
    {
        if (!File.Exists(configPath))
        {
            skipped.Add($"{configPath} (not found)");
            return;
        }

        try
        {
            string content = File.ReadAllText(configPath);

            // Check if neuralcore is actually configured
            if (!content.Contains("neuralcore", StringComparison.OrdinalIgnoreCase) &&
                !content.Contains("temperai-neuralcore", StringComparison.OrdinalIgnoreCase))
            {
                skipped.Add($"{configPath} (NeuralCore not configured)");
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using JsonDocument doc = JsonDocument.Parse(content);
            var root = new Dictionary<string, object>();

            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                root[property.Name] = JsonElementToObject(property.Value);
            }

            bool modified = false;

            switch (format)
            {
                case "opencode":
                    // { "mcp": { "neuralcore": {...} } }
                    if (root.TryGetValue("mcp", out var mcpObj) &&
                        mcpObj is Dictionary<string, object> mcpDict)
                    {
                        if (mcpDict.Remove("neuralcore"))
                        {
                            modified = true;

                            // If mcp is now empty, remove it too
                            if (mcpDict.Count == 0)
                            {
                                root.Remove("mcp");
                            }
                        }
                    }
                    break;
            }

            if (modified)
            {
                string newContent = JsonSerializer.Serialize(root, options);
                File.WriteAllText(configPath, newContent);
                removed.Add($"{configPath} (neuralcore entry removed)");
            }
            else
            {
                skipped.Add($"{configPath} (neuralcore entry not found)");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to update {configPath}: {ex.Message}");
        }
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

    private void ScheduleDeleteViaScript(string targetPath)
    {
        string tempScriptPath = Path.Combine(Path.GetTempPath(), "temper-ai-uninstall.ps1");

        string scriptContent = $@"
            Start-Sleep -Milliseconds 1500
            try {{
                Remove-Item -Path '{targetPath}' -Force -ErrorAction Stop
                Write-Host 'TemperAI CLI removed successfully.'
            }} catch {{
                Write-Host 'Error removing TemperAI CLI: $_'
            }}

            # Clean up parent directory if empty
            $parentDir = Split-Path -Path '{targetPath}' -Parent
            if (Test-Path $parentDir) {{
                $items = Get-ChildItem -Path $parentDir -Force
                if ($items.Count -eq 0) {{
                    Remove-Item -Path $parentDir -Force -ErrorAction SilentlyContinue
                }}
            }}

            Remove-Item -Path '{tempScriptPath}' -Force -ErrorAction SilentlyContinue
        ";

        File.WriteAllText(tempScriptPath, scriptContent);

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{tempScriptPath}\"",
            UseShellExecute = true
        };

        System.Diagnostics.Process.Start(startInfo);
    }
}
