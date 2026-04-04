using TemperAI.NeuralCore.Domain.Common.Primitives;
using TemperAI.NeuralCore.Domain.Entities.Sessions.Enums;

namespace TemperAI.NeuralCore.Domain.Entities.Sessions;

public sealed class Session : Entity<Guid>
{
    public class Rules
    {
        public const int PROJECT_MAX_LENGTH = 200;
        public const int DIRECTORY_MAX_LENGTH = 500;
        public const int SUMMARY_MAX_LENGTH = 2000;
    }

    public string Project { get; private set; } = string.Empty;
    public string Directory { get; private set; } = string.Empty;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public SessionStatus Status { get; private set; }

    private Session()
    {
    }

    public static (List<string> Errors, Session? Session) Create(
        string project,
        string directory)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(project))
        {
            errors.Add("Project is required");
        }
        else if (project.Length > Rules.PROJECT_MAX_LENGTH)
        {
            errors.Add($"Project cannot exceed {Rules.PROJECT_MAX_LENGTH} characters");
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            errors.Add("Directory is required");
        }
        else if (directory.Length > Rules.DIRECTORY_MAX_LENGTH)
        {
            errors.Add($"Directory cannot exceed {Rules.DIRECTORY_MAX_LENGTH} characters");
        }

        if (errors.Count > 0)
        {
            return (errors, null);
        }

        Session session = new()
        {
            Id = Guid.NewGuid(),
            Project = project,
            Directory = directory,
            StartedAt = DateTime.UtcNow,
            Status = SessionStatus.Active
        };

        return ([], session);
    }

    public (List<string> Errors, bool Updated) UpdateSummary(string newSummary)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(newSummary))
        {
            errors.Add("Summary is required");
        }
        else if (newSummary.Length > Rules.SUMMARY_MAX_LENGTH)
        {
            errors.Add($"Summary cannot exceed {Rules.SUMMARY_MAX_LENGTH} characters");
        }

        if (errors.Count > 0)
        {
            return (errors, false);
        }

        if (Summary == newSummary)
        {
            return ([], false);
        }

        Summary = newSummary;
        return ([], true);
    }

    public (List<string> Errors, bool Updated) Complete()
    {
        if (Status != SessionStatus.Active)
        {
            return (["Only active sessions can be completed"], false);
        }

        Status = SessionStatus.Completed;
        EndedAt = DateTime.UtcNow;
        return ([], true);
    }

    public (List<string> Errors, bool Updated) Abandon()
    {
        if (Status != SessionStatus.Active)
        {
            return (["Only active sessions can be abandoned"], false);
        }

        Status = SessionStatus.Abandoned;
        EndedAt = DateTime.UtcNow;
        return ([], true);
    }
}
