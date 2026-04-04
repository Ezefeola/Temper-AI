using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Domain.Entities.Sessions;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly NeuralCoreDbContext _dbContext;

    public SessionRepository(NeuralCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Session?> GetByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .FirstOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);
    }

    public async Task<Session?> GetByIdAsNoTrackingAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                session => session.Id == sessionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Session>> GetByProjectAsync(
        string project,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(session => session.Project == project)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Session session,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Sessions.AddAsync(session, cancellationToken);
    }
}
