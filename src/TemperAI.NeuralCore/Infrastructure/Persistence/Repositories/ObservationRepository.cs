using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Domain.Entities.Observations;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository : IObservationRepository
{
    private readonly NeuralCoreDbContext _dbContext;

    public ObservationRepository(NeuralCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Observation?> GetByIdAsync(
        int observationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .FirstOrDefaultAsync(
                obs => obs.Id == observationId,
                cancellationToken);
    }

    public async Task<Observation?> GetByIdAsNoTrackingAsync(
        int observationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                obs => obs.Id == observationId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .AsNoTracking()
            .Where(obs => obs.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetByProjectAsync(
        string project,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .AsNoTracking()
            .Where(obs => obs.Project == project)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetByTopicKeyAsync(
        string topicKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .AsNoTracking()
            .Where(obs => obs.TopicKey == topicKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Observation>> GetAllAsNoTrackingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Observations
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Observation observation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Observations.AddAsync(observation, cancellationToken);
    }
}
