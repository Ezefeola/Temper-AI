using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

namespace TemperAI.NeuralCore.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly NeuralCoreDbContext _dbContext;

    public ISessionRepository SessionRepository { get; }
    public IObservationRepository ObservationRepository { get; }

    public UnitOfWork(
        NeuralCoreDbContext dbContext,
        ISessionRepository sessionRepository,
        IObservationRepository observationRepository)
    {
        _dbContext = dbContext;
        SessionRepository = sessionRepository;
        ObservationRepository = observationRepository;
    }

    public async Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            int rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);

            return new SaveResult
            {
                IsSuccess = true,
                RowsAffected = rowsAffected
            };
        }
        catch (DbUpdateException dbUpdateException)
        {
            return new SaveResult
            {
                IsSuccess = false,
                ErrorMessage = dbUpdateException.InnerException?.Message
                    ?? dbUpdateException.Message
            };
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
