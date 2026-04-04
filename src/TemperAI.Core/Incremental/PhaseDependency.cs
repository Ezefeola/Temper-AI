using System.IO;

namespace TemperAI.Core.Incremental;

public sealed class PhaseDependency
{
    public string PhaseName { get; init; } = string.Empty;
    public string[] DependsOn { get; init; } = [];
    public string[] TrackedFiles { get; init; } = [];
    public string Status { get; init; } = "unknown";
}
