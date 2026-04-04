using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemperAI.NeuralCore.Domain.Entities.Sessions;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Property(session => session.Project)
            .HasMaxLength(Session.Rules.PROJECT_MAX_LENGTH)
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(session => session.Directory)
            .HasMaxLength(Session.Rules.DIRECTORY_MAX_LENGTH)
            .HasColumnType("nvarchar")
            .IsRequired();

        builder.Property(session => session.Summary)
            .HasMaxLength(Session.Rules.SUMMARY_MAX_LENGTH)
            .HasColumnType("nvarchar");

        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(session => session.StartedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(session => session.EndedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(session => session.Project);
    }
}
