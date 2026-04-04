namespace TemperAI.Core.Skills;

public sealed class SkillInfo
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] Tags { get; init; } = [];
    public string Path { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public long TotalSizeBytes { get; init; }
}
