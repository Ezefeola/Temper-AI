namespace TemperAI.Core.Models;

public sealed class ReleaseManifest
{
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Channel { get; init; } = "stable";
    public DateTimeOffset PublishedAt { get; init; }
    public CliManifest Cli { get; init; } = new();
    public AssetsManifest Assets { get; init; } = new();
    public CompatibilityManifest Compatibility { get; init; } = new();
}

public sealed class CliManifest
{
    public string Version { get; init; } = string.Empty;
    public List<CliPlatformManifest> Platforms { get; init; } = [];
}

public sealed class CliPlatformManifest
{
    public string Rid { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
}

public sealed class AssetsManifest
{
    public string Version { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
}

public sealed class CompatibilityManifest
{
    public string CliVersion { get; init; } = string.Empty;
    public string AssetsVersion { get; init; } = string.Empty;
    public string UpdateMode { get; init; } = string.Empty;
}
