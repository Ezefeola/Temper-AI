using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Infrastructure.Persistence.Repositories;

namespace TemperAI.NeuralCore.Infrastructure.Persistence;

public interface IUnitOfWork : IDisposable
{
    ISessionRepository SessionRepository { get; }
    IObservationRepository ObservationRepository { get; }

    Task<SaveResult> CompleteAsync(CancellationToken cancellationToken = default);
}
