using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemperAI.NeuralCore.Domain.Entities.Observations;
using TemperAI.NeuralCore.Domain.Entities.Sessions;

namespace TemperAI.NeuralCore.Infrastructure.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.HasKey(obs => obs.Id);

        builder.Property(obs => obs.Id)
            .ValueGeneratedOnAdd();

        builder.Property(obs => obs.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(obs => obs.Title)
            .HasMaxLength(Observation.Rules.TITLE_MAX_LENGTH)
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(obs => obs.Content)
            .HasMaxLength(Observation.Rules.CONTENT_MAX_LENGTH)
            .HasColumnType("nvarchar")
            .IsRequired();

        builder.Property(obs => obs.Project)
            .HasMaxLength(Observation.Rules.PROJECT_MAX_LENGTH)
            .HasColumnType("varchar")
            .IsRequired();

        builder.Property(obs => obs.TopicKey)
            .HasMaxLength(Observation.Rules.TOPIC_KEY_MAX_LENGTH)
            .HasColumnType("varchar");

        builder.Property(obs => obs.RevisionCount)
            .IsRequired();

        builder.Property(obs => obs.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(obs => obs.UpdatedAt)
            .HasColumnType("datetime2");

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(obs => obs.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(obs => obs.SessionId);
        builder.HasIndex(obs => obs.TopicKey);
        builder.HasIndex(obs => obs.Project);
    }
}
