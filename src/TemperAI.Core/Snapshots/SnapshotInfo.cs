using System.IO;

namespace TemperAI.Core.Snapshots;

public sealed class SnapshotInfo
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Phase { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public long TotalSizeBytes { get; init; }
}
