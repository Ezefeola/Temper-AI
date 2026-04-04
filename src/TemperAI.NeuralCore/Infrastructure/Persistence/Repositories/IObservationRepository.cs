using TemperAI.NeuralCore.Domain.Entities.Observations;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

public interface IObservationRepository
{
    Task<Observation?> GetByIdAsync(
        int observationId,
        CancellationToken cancellationToken = default);

    Task<Observation?> GetByIdAsNoTrackingAsync(
        int observationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetByProjectAsync(
        string project,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Observation>> GetByTopicKeyAsync(
        string topicKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Observation observation,
        CancellationToken cancellationToken = default);
}
