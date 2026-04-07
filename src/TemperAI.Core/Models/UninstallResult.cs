namespace TemperAI.Core.Models;

public sealed class UninstallResult
{
    public string Component { get; init; } = string.Empty;
    public List<string> Removed { get; init; } = [];
    public List<string> Skipped { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public bool IsSuccess { get; init; }

    public string Summary()
    {
        if (!IsSuccess)
        {
            return $"Error desinstalando {Component}";
        }

        return $"{Component} — {Removed.Count} archivos removidos, {Skipped.Count} omitidos";
    }
}
