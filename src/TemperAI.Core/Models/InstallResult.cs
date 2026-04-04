namespace TemperAI.Core.Models;

public sealed class InstallResult
{
    public AgentTarget Target { get; init; } = new();
    public List<string> Installed { get; init; } = [];
    public List<string> Skipped { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public bool IsSuccess { get; init; }

    public string Summary()
    {
        if (!IsSuccess)
        {
            return $"Error instalando para {Target.Name}";
        }

        return $"{Target.Name} — {Installed.Count} archivos instalados, {Skipped.Count} omitidos";
    }
}