using System.Diagnostics;

namespace TemperAI.Core.Models;

public sealed class NeuralCoreStatus
{
    public bool IsPublished { get; init; }
    public string ExePath { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public DateTime? LastModified { get; init; }

    public bool IsRunning { get; init; }
    public int? ProcessId { get; init; }
    public TimeSpan? RunningDuration { get; init; }

    public bool IsConfiguredForOpenCode { get; init; }
    public bool IsConfiguredForCopilot { get; init; }
    public bool IsConfiguredForClaude { get; init; }

    public string? LastError { get; init; }
    public DateTime? LastErrorTime { get; init; }

    public string? SuggestedAction { get; init; }

    public string FileSizeDisplay
    {
        get
        {
            if (!FileSizeBytes.HasValue) return "N/A";
            long bytes = FileSizeBytes.Value;
            if (bytes > 1_000_000) return $"{bytes / 1_000_000.0:F1} MB";
            return $"{bytes / 1_000.0:F1} KB";
        }
    }

    public string RunningDurationDisplay
    {
        get
        {
            if (!RunningDuration.HasValue) return "N/A";
            TimeSpan duration = RunningDuration.Value;
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";
            return $"{duration.Seconds}s";
        }
    }
}

public sealed class NeuralCoreStartResult
{
    public bool Success { get; init; }
    public int? ProcessId { get; init; }
    public string? ErrorMessage { get; init; }
    public string LogPath { get; init; } = string.Empty;
}

public sealed class NeuralCoreStopResult
{
    public bool Success { get; init; }
    public int? KilledProcessId { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class NeuralCoreHealthCheckResult
{
    public bool IsHealthy { get; init; }
    public bool Exists { get; init; }
    public bool CanStart { get; init; }
    public bool DatabaseAccessible { get; init; }
    public bool McpConfigured { get; init; }
    public List<string> Issues { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
}

public sealed class DoctorCheckResult
{
    public string CheckName { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string Details { get; init; } = string.Empty;
    public string? FixSuggestion { get; init; }
}

public sealed class DoctorResult
{
    public bool AllPassed { get; init; }
    public List<DoctorCheckResult> Checks { get; init; } = [];
    public bool CanRepair { get; init; }
}

public sealed class RepairResult
{
    public bool Success { get; init; }
    public List<string> ActionsPerformed { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}