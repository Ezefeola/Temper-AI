namespace TemperAI.Core.Models;

public sealed class InstallMetadata
{
    public string Channel { get; init; } = "stable";
    public string SourceMode { get; init; } = "remote";
    public string ManifestUrl { get; init; } = string.Empty;
    public string InstalledCliVersion { get; init; } = string.Empty;
    public string InstalledAssetsVersion { get; init; } = string.Empty;
    public DateTimeOffset InstalledAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
}
