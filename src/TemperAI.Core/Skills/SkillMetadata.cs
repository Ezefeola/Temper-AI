using System.IO;

namespace TemperAI.Core.Skills;

public sealed class SkillMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string[] Dependencies { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public string License { get; init; } = "MIT";
    public string RepositoryUrl { get; init; } = string.Empty;
}
