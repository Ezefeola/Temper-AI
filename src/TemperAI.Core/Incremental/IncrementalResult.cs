using System.IO;

namespace TemperAI.Core.Incremental;

public sealed class IncrementalResult
{
    public bool RequiresRerun { get; init; }
    public List<string> AffectedPhases { get; init; } = [];
    public List<string> ChangedFiles { get; init; } = [];
    public List<string> UnchangedPhases { get; init; } = [];
    public string Message { get; init; } = string.Empty;

    public static IncrementalResult NoChanges()
    {
        return new IncrementalResult
        {
            RequiresRerun = false,
            Message = "No changes detected. No phases need to re-run."
        };
    }

    public static IncrementalResult ChangesDetected(
        List<string> changedFiles,
        List<string> affectedPhases,
        List<string> unchangedPhases)
    {
        return new IncrementalResult
        {
            RequiresRerun = true,
            ChangedFiles = changedFiles,
            AffectedPhases = affectedPhases,
            UnchangedPhases = unchangedPhases,
            Message = $"{changedFiles.Count} file(s) changed. {affectedPhases.Count} phase(s) need to re-run."
        };
    }
}
