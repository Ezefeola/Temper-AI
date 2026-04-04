using TemperAI.NeuralCore.Domain.Entities.Sessions;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<Session?> GetByIdAsNoTrackingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Session>> GetByProjectAsync(
        string project,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Session session,
        CancellationToken cancellationToken = default);
}
