using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using TemperAI.Installer;

namespace TemperAI.Cli.Services;

public sealed class NeuralCoreService
{
    private const string LockFileName = "neuralcore.lock";
    private const string LogFileName = "neuralcore.log";

    public NeuralCoreStatus GetStatus()
    {
        string exePath = NeuralCoreInstallerService.GetNeuralCoreExePath();
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();

        bool isPublished = File.Exists(exePath);
        long? fileSize = isPublished ? new FileInfo(exePath).Length : null;
        DateTime? lastModified = isPublished ? File.GetLastWriteTime(exePath) : null;

        int? runningPid = GetRunningProcessId();
        TimeSpan? runningDuration = GetProcessRunningDuration(runningPid);

        bool openCodeConfigured = IsMcpConfiguredForAgent("opencode");
        bool copilotConfigured = IsMcpConfiguredForAgent("copilot");
        bool claudeConfigured = IsMcpConfiguredForAgent("claude");

        string? suggestedAction = GetSuggestedAction(isPublished, runningPid, openCodeConfigured);

        return new NeuralCoreStatus
        {
            IsPublished = isPublished,
            ExePath = exePath,
            FileSizeBytes = fileSize,
            LastModified = lastModified,
            IsRunning = runningPid.HasValue,
            ProcessId = runningPid,
            RunningDuration = runningDuration,
            IsConfiguredForOpenCode = openCodeConfigured,
            IsConfiguredForCopilot = copilotConfigured,
            IsConfiguredForClaude = claudeConfigured,
            SuggestedAction = suggestedAction
        };
    }

    public NeuralCoreStartResult Start()
    {
        string exePath = NeuralCoreInstallerService.GetNeuralCoreExePath();
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string logPath = Path.Combine(installPath, "logs");

        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }

        if (!File.Exists(exePath))
        {
            return new NeuralCoreStartResult
            {
                Success = false,
                ErrorMessage = $"NeuralCore no encontrado en: {exePath}. Ejecutá 'temper-ai neuralcore --publish' primero."
            };
        }

        int? existingPid = GetRunningProcessId();
        if (existingPid.HasValue)
        {
            return new NeuralCoreStartResult
            {
                Success = false,
                ErrorMessage = $"NeuralCore ya está ejecutándose con PID: {existingPid.Value}. Usá 'temper-ai neuralcore --restart' para reiniciar."
            };
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
                WorkingDirectory = installPath
            };

            using Process process = Process.Start(startInfo);

            if (process is null)
            {
                return new NeuralCoreStartResult
                {
                    Success = false,
                    ErrorMessage = "No se pudo iniciar el proceso de NeuralCore."
                };
            }

            SaveProcessId(process.Id);
            EnsureLogDirectory();

            return new NeuralCoreStartResult
            {
                Success = true,
                ProcessId = process.Id,
                LogPath = Path.Combine(logPath, LogFileName)
            };
        }
        catch (Exception ex)
        {
            return new NeuralCoreStartResult
            {
                Success = false,
                ErrorMessage = $"Error al iniciar NeuralCore: {ex.Message}"
            };
        }
    }

    public NeuralCoreStopResult Stop()
    {
        int? pid = GetRunningProcessId();

        if (!pid.HasValue)
        {
            return new NeuralCoreStopResult
            {
                Success = true,
                ErrorMessage = "NeuralCore no está en ejecución."
            };
        }

        try
        {
            using Process? process = Process.GetProcessById(pid.Value);
            process.Kill(true);

            ClearProcessId();

            return new NeuralCoreStopResult
            {
                Success = true,
                KilledProcessId = pid.Value
            };
        }
        catch (Exception ex)
        {
            ClearProcessId();
            return new NeuralCoreStopResult
            {
                Success = false,
                KilledProcessId = pid.Value,
                ErrorMessage = $"Error al detener NeuralCore: {ex.Message}"
            };
        }
    }

    public NeuralCoreStartResult Restart()
    {
        Stop();
        Thread.Sleep(500);
        return Start();
    }

    public NeuralCoreHealthCheckResult HealthCheck()
    {
        var issues = new List<string>();
        var recommendations = new List<string>();

        string exePath = NeuralCoreInstallerService.GetNeuralCoreExePath();
        bool exists = File.Exists(exePath);

        if (!exists)
        {
            issues.Add("NeuralCore.exe no está publicado. Ejecutá 'temper-ai neuralcore --publish'");
            recommendations.Add("Ejecutá: temper-ai neuralcore --publish");
        }

        bool canStart = false;
        if (exists)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--test-ping",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using Process? testProcess = Process.Start(startInfo);
                if (testProcess is not null)
                {
                    bool exited = testProcess.WaitForExit(5000);
                    canStart = exited && testProcess.ExitCode == 0;

                    if (!canStart && !exited)
                    {
                        testProcess.Kill();
                        canStart = true;
                    }
                }
            }
            catch
            {
                canStart = false;
            }
        }

        bool databaseAccessible = false;
        if (exists)
        {
            string dbPath = Path.Combine(
                NeuralCoreInstallerService.GetNeuralCoreInstallPath(),
                "neural.db");

            if (File.Exists(dbPath))
            {
                try
                {
                    using var connection = new SqliteConnection($"Data Source={dbPath}");
                    connection.Open();
                    databaseAccessible = true;
                }
                catch
                {
                    databaseAccessible = false;
                }
            }
            else
            {
                databaseAccessible = true;
            }
        }

        IReadOnlyList<AgentTarget> targets = AgentTargets.Supported();
        bool mcpConfigured = targets.All(t =>
            File.Exists(t.McpConfigFile) && IsNeuralCoreInConfig(t.McpConfigFile));

        if (!mcpConfigured)
        {
            issues.Add("MCP no está configurado en algunos agentes.");
            recommendations.Add("Ejecutá: temper-ai neuralcore --install");
        }

        bool isHealthy = exists && canStart && mcpConfigured;

        return new NeuralCoreHealthCheckResult
        {
            IsHealthy = isHealthy,
            Exists = exists,
            CanStart = canStart,
            DatabaseAccessible = databaseAccessible,
            McpConfigured = mcpConfigured,
            Issues = issues,
            Recommendations = recommendations
        };
    }

    public DoctorResult RunDoctor()
    {
        var checks = new List<DoctorCheckResult>();

        checks.Add(CheckPathVariable());
        checks.Add(CheckNeuralCorePublished());
        checks.Add(CheckNeuralCoreRunning());
        checks.Add(CheckMcpConfiguration());
        checks.Add(CheckDatabasePermissions());

        bool allPassed = checks.All(c => c.Passed);

        return new DoctorResult
        {
            AllPassed = allPassed,
            Checks = checks,
            CanRepair = checks.Any(c => !c.Passed && c.FixSuggestion is not null)
        };
    }

    public RepairResult Repair()
    {
        var actions = new List<string>();
        var errors = new List<string>();

        DoctorResult doctor = RunDoctor();

        foreach (DoctorCheckResult check in doctor.Checks.Where(c => !c.Passed && c.FixSuggestion is not null))
        {
            try
            {
                switch (check.CheckName)
                {
                    case "PATH":
                        RepairPathVariable();
                        actions.Add("PATH actualizado");
                        break;

                    case "NeuralCorePublished":
                        NeuralCoreInstallerService.PublishNeuralCore();
                        actions.Add("NeuralCore publicado");
                        break;

                    case "McpConfiguration":
                        InstallMcpForAllAgents();
                        actions.Add("MCP configurado en agentes");
                        break;

                    case "DatabasePermissions":
                        EnsureDatabaseWritable();
                        actions.Add("Permisos de base de datos verificados");
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error reparando {check.CheckName}: {ex.Message}");
            }
        }

        return new RepairResult
        {
            Success = errors.Count == 0,
            ActionsPerformed = actions,
            Errors = errors
        };
    }

    private static DoctorCheckResult CheckPathVariable()
    {
        string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string parentPath = Path.GetDirectoryName(installPath) ?? installPath;

        bool hasPath = userPath.Contains(installPath, StringComparison.OrdinalIgnoreCase)
            || userPath.Contains(parentPath, StringComparison.OrdinalIgnoreCase);

        return new DoctorCheckResult
        {
            CheckName = "PATH",
            Passed = hasPath,
            Details = hasPath
                ? "TemperAI está en el PATH del usuario"
                : "TemperAI no está en el PATH del usuario",
            FixSuggestion = hasPath ? null : "Ejecutá 'temper-ai setup' para agregar al PATH"
        };
    }

    private static DoctorCheckResult CheckNeuralCorePublished()
    {
        string exePath = NeuralCoreInstallerService.GetNeuralCoreExePath();
        bool published = File.Exists(exePath);

        return new DoctorCheckResult
        {
            CheckName = "NeuralCorePublished",
            Passed = published,
            Details = published
                ? $"NeuralCore publicado en: {exePath}"
                : "NeuralCore no está publicado",
            FixSuggestion = published ? null : "Ejecutá 'temper-ai neuralcore --publish'"
        };
    }

    private static DoctorCheckResult CheckNeuralCoreRunning()
    {
        int? pid = GetRunningProcessId();
        bool running = pid.HasValue;

        return new DoctorCheckResult
        {
            CheckName = "NeuralCoreRunning",
            Passed = running,
            Details = running
                ? $"NeuralCore ejecutándose con PID: {pid}"
                : "NeuralCore no está en ejecución",
            FixSuggestion = running ? null : "Ejecutá 'temper-ai neuralcore --start'"
        };
    }

    private static DoctorCheckResult CheckMcpConfiguration()
    {
        IReadOnlyList<AgentTarget> targets = AgentTargets.Supported();
        var configured = new List<string>();
        var notConfigured = new List<string>();

        foreach (AgentTarget target in targets)
        {
            if (File.Exists(target.McpConfigFile) && IsNeuralCoreInConfig(target.McpConfigFile))
            {
                configured.Add(target.Name);
            }
            else
            {
                notConfigured.Add(target.Name);
            }
        }

        bool allConfigured = notConfigured.Count == 0;

        return new DoctorCheckResult
        {
            CheckName = "McpConfiguration",
            Passed = allConfigured,
            Details = allConfigured
                ? "MCP configurado en todos los agentes"
                : $"Falta configurar en: {string.Join(", ", notConfigured)}",
            FixSuggestion = allConfigured ? null : "Ejecutá 'temper-ai neuralcore --install'"
        };
    }

    private static DoctorCheckResult CheckDatabasePermissions()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string dbPath = Path.Combine(installPath, "neural.db");

        bool canWrite = true;
        string details;

        try
        {
            if (File.Exists(dbPath))
            {
                using var testConnection = new SqliteConnection($"Data Source={dbPath}");
                testConnection.Open();
                details = "Base de datos accesible";
            }
            else
            {
                details = "Base de datos no existe todavía (se creará al iniciar)";
            }
        }
        catch (Exception ex)
        {
            canWrite = false;
            details = $"Error de permisos: {ex.Message}";
        }

        return new DoctorCheckResult
        {
            CheckName = "DatabasePermissions",
            Passed = canWrite,
            Details = details,
            FixSuggestion = canWrite ? null : "Verificar permisos de la carpeta NeuralCore"
        };
    }

    private static void RepairPathVariable()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string parentPath = Path.GetDirectoryName(installPath) ?? installPath;

        string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;

        if (!userPath.Contains(parentPath, StringComparison.OrdinalIgnoreCase))
        {
            string newPath = string.IsNullOrEmpty(userPath) ? parentPath : $"{userPath};{parentPath}";
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
        }
    }

    private static void InstallMcpForAllAgents()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        var installer = new NeuralCoreInstallerService();

        foreach (AgentTarget target in AgentTargets.Supported())
        {
            installer.InstallNeuralCore(target, installPath);
        }
    }

    private static void EnsureDatabaseWritable()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string dbPath = Path.Combine(installPath, "neural.db");

        string? directory = Path.GetDirectoryName(dbPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static int? GetRunningProcessId()
    {
        string lockPath = GetLockFilePath();

        if (!File.Exists(lockPath))
        {
            return null;
        }

        try
        {
            string content = File.ReadAllText(lockPath);
            if (int.TryParse(content.Trim(), out int pid))
            {
                try
                {
                    using Process process = Process.GetProcessById(pid);
                    if (!process.HasExited)
                    {
                        return pid;
                    }
                }
                catch
                {
                    ClearProcessId();
                }
            }
        }
        catch
        {
        }

        ClearProcessId();
        return null;
    }

    private static TimeSpan? GetProcessRunningDuration(int? pid)
    {
        if (!pid.HasValue)
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById(pid.Value);
            return DateTime.Now - process.StartTime;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveProcessId(int pid)
    {
        string lockPath = GetLockFilePath();
        string? directory = Path.GetDirectoryName(lockPath);

        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(lockPath, pid.ToString());
    }

    private static void ClearProcessId()
    {
        string lockPath = GetLockFilePath();

        if (File.Exists(lockPath))
        {
            try
            {
                File.Delete(lockPath);
            }
            catch
            {
            }
        }
    }

    private static string GetLockFilePath()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        return Path.Combine(installPath, LockFileName);
    }

    private static void EnsureLogDirectory()
    {
        string installPath = NeuralCoreInstallerService.GetNeuralCoreInstallPath();
        string logPath = Path.Combine(installPath, "logs");

        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }
    }

    private static bool IsMcpConfiguredForAgent(string agentId)
    {
        AgentTarget? target = AgentTargets.FindById(agentId);

        if (target is null || !File.Exists(target.McpConfigFile))
        {
            return false;
        }

        return IsNeuralCoreInConfig(target.McpConfigFile);
    }

    private static bool IsNeuralCoreInConfig(string configPath)
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

    private static string? GetSuggestedAction(bool isPublished, int? runningPid, bool openCodeConfigured)
    {
        if (!isPublished)
        {
            return "Ejecutá 'temper-ai neuralcore --publish' para publicar NeuralCore";
        }

        if (runningPid.HasValue)
        {
            return "NeuralCore está ejecutándose correctamente";
        }

        if (!openCodeConfigured)
        {
            return "Ejecutá 'temper-ai neuralcore --install' para configurar MCP";
        }

        return "Ejecutá 'temper-ai neuralcore --start' para iniciar NeuralCore";
    }
}