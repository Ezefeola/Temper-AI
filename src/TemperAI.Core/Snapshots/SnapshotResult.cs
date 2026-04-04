using System.IO;

namespace TemperAI.Core.Snapshots;

public sealed class SnapshotResult
{
    public bool IsSuccess { get; init; }
    public string SnapshotPath { get; init; } = string.Empty;
    public List<string> Files { get; init; } = [];
    public string Error { get; init; } = string.Empty;

    public static SnapshotResult Success(string path, List<string> files)
    {
        return new SnapshotResult { IsSuccess = true, SnapshotPath = path, Files = files };
    }

    public static SnapshotResult Failure(string error)
    {
        return new SnapshotResult { IsSuccess = false, Error = error };
    }
}
