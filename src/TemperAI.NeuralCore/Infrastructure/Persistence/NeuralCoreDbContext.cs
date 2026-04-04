using Microsoft.EntityFrameworkCore;
using TemperAI.NeuralCore.Domain.Entities.Observations;
using TemperAI.NeuralCore.Domain.Entities.Sessions;

namespace TemperAI.NeuralCore.Infrastructure.Persistence;

public sealed class NeuralCoreDbContext : DbContext
{
    public NeuralCoreDbContext(DbContextOptions<NeuralCoreDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Observation> Observations => Set<Observation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NeuralCoreDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
