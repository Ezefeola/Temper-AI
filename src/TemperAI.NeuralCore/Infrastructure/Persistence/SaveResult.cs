using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

namespace TemperAI.NeuralCore.Infrastructure.Persistence;

public sealed class SaveResult
{
    public bool IsSuccess { get; init; }
    public int RowsAffected { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
