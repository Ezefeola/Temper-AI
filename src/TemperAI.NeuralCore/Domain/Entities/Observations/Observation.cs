using TemperAI.NeuralCore.Domain.Common.Primitives;
using TemperAI.NeuralCore.Domain.Entities.Observations.Enums;

namespace TemperAI.NeuralCore.Domain.Entities.Observations;

public sealed class Observation : Entity<int>
{
    public class Rules
    {
        public const int TITLE_MAX_LENGTH = 200;
        public const int CONTENT_MAX_LENGTH = 4000;
        public const int PROJECT_MAX_LENGTH = 200;
        public const int TOPIC_KEY_MAX_LENGTH = 200;
    }

    public Guid SessionId { get; private set; }
    public ObservationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Project { get; private set; } = string.Empty;
    public string? TopicKey { get; private set; }
    public int RevisionCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Observation()
    {
    }

    public static (List<string> Errors, Observation? Observation) Create(
        Guid sessionId,
        ObservationType type,
        string title,
        string content,
        string project,
        string? topicKey)
    {
        List<string> errors = [];

        if (sessionId == Guid.Empty)
        {
            errors.Add("SessionId is required");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Title is required");
        }
        else if (title.Length > Rules.TITLE_MAX_LENGTH)
        {
            errors.Add($"Title cannot exceed {Rules.TITLE_MAX_LENGTH} characters");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            errors.Add("Content is required");
        }
        else if (content.Length > Rules.CONTENT_MAX_LENGTH)
        {
            errors.Add($"Content cannot exceed {Rules.CONTENT_MAX_LENGTH} characters");
        }

        if (string.IsNullOrWhiteSpace(project))
        {
            errors.Add("Project is required");
        }
        else if (project.Length > Rules.PROJECT_MAX_LENGTH)
        {
            errors.Add($"Project cannot exceed {Rules.PROJECT_MAX_LENGTH} characters");
        }

        if (!string.IsNullOrEmpty(topicKey) && topicKey.Length > Rules.TOPIC_KEY_MAX_LENGTH)
        {
            errors.Add($"TopicKey cannot exceed {Rules.TOPIC_KEY_MAX_LENGTH} characters");
        }

        if (errors.Count > 0)
        {
            return (errors, null);
        }

        Observation observation = new()
        {
            Id = 0,
            SessionId = sessionId,
            Type = type,
            Title = title,
            Content = content,
            Project = project,
            TopicKey = topicKey,
            RevisionCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        return ([], observation);
    }

    public (List<string> Errors, bool Updated) UpdateContent(
        string newContent,
        string? newTopicKey)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(newContent))
        {
            errors.Add("Content is required");
        }
        else if (newContent.Length > Rules.CONTENT_MAX_LENGTH)
        {
            errors.Add($"Content cannot exceed {Rules.CONTENT_MAX_LENGTH} characters");
        }

        if (!string.IsNullOrEmpty(newTopicKey) && newTopicKey.Length > Rules.TOPIC_KEY_MAX_LENGTH)
        {
            errors.Add($"TopicKey cannot exceed {Rules.TOPIC_KEY_MAX_LENGTH} characters");
        }

        if (errors.Count > 0)
        {
            return (errors, false);
        }

        bool contentChanged = Content != newContent;
        bool topicKeyChanged = TopicKey != newTopicKey;

        if (!contentChanged && !topicKeyChanged)
        {
            return ([], false);
        }

        Content = newContent;
        TopicKey = newTopicKey;
        RevisionCount++;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }

    public (List<string> Errors, bool Updated) UpdateTitle(string newTitle)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            errors.Add("Title is required");
        }
        else if (newTitle.Length > Rules.TITLE_MAX_LENGTH)
        {
            errors.Add($"Title cannot exceed {Rules.TITLE_MAX_LENGTH} characters");
        }

        if (errors.Count > 0)
        {
            return (errors, false);
        }

        if (Title == newTitle)
        {
            return ([], false);
        }

        Title = newTitle;
        UpdatedAt = DateTime.UtcNow;
        return ([], true);
    }
}
