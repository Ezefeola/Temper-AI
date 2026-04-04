namespace TemperAI.Core.Models;

public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}