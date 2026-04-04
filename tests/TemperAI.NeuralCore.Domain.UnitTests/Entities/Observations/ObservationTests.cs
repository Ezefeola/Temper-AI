using TemperAI.NeuralCore.Domain.Entities.Observations;
using TemperAI.NeuralCore.Domain.Entities.Observations.Enums;

namespace TemperAI.NeuralCore.Domain.UnitTests.Entities.Observations;

public sealed class ObservationTests
{
    private static readonly Guid _validSessionId = Guid.NewGuid();

    [Fact]
    public void Create_ValidObservation_ReturnsSuccess()
    {
        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix null reference",
            "Fixed null reference in ProductController",
            "TestProject",
            "null-ref-fix");

        Assert.Empty(errors);
        Assert.NotNull(observation);
        Assert.Equal(_validSessionId, observation!.SessionId);
        Assert.Equal(ObservationType.Bugfix, observation.Type);
        Assert.Equal("Fix null reference", observation.Title);
        Assert.Equal("Fixed null reference in ProductController", observation.Content);
        Assert.Equal("TestProject", observation.Project);
        Assert.Equal("null-ref-fix", observation.TopicKey);
        Assert.Equal(0, observation.RevisionCount);
        Assert.NotEqual(default, observation.CreatedAt);
        Assert.Null(observation.UpdatedAt);
    }

    [Fact]
    public void Create_EmptySessionId_ReturnsError()
    {
        var (errors, observation) = Observation.Create(
            Guid.Empty,
            ObservationType.Bugfix,
            "Fix null reference",
            "Content",
            "TestProject",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
        Assert.Contains(errors, e => e.Contains("session", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_EmptyTitle_ReturnsError()
    {
        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "",
            "Content",
            "TestProject",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
        Assert.Contains(errors, e => e.Contains("title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_TitleExceedsMaxLength_ReturnsError()
    {
        string longTitle = new string('a', Observation.Rules.TITLE_MAX_LENGTH + 1);

        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            longTitle,
            "Content",
            "TestProject",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
    }

    [Fact]
    public void Create_EmptyContent_ReturnsError()
    {
        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "",
            "TestProject",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
        Assert.Contains(errors, e => e.Contains("content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_ContentExceedsMaxLength_ReturnsError()
    {
        string longContent = new string('a', Observation.Rules.CONTENT_MAX_LENGTH + 1);

        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            longContent,
            "TestProject",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
    }

    [Fact]
    public void Create_EmptyProject_ReturnsError()
    {
        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Content",
            "",
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
    }

    [Fact]
    public void Create_ProjectExceedsMaxLength_ReturnsError()
    {
        string longProject = new string('a', Observation.Rules.PROJECT_MAX_LENGTH + 1);

        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Content",
            longProject,
            null);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
    }

    [Fact]
    public void Create_TopicKeyExceedsMaxLength_ReturnsError()
    {
        string longTopicKey = new string('a', Observation.Rules.TOPIC_KEY_MAX_LENGTH + 1);

        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Content",
            "TestProject",
            longTopicKey);

        Assert.NotEmpty(errors);
        Assert.Null(observation);
    }

    [Fact]
    public void Create_NullTopicKey_Succeeds()
    {
        var (errors, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Content",
            "TestProject",
            null);

        Assert.Empty(errors);
        Assert.NotNull(observation);
        Assert.Null(observation!.TopicKey);
    }

    [Fact]
    public void Create_MultipleErrors_ReturnsAllErrors()
    {
        var (errors, observation) = Observation.Create(
            Guid.Empty,
            ObservationType.Bugfix,
            "",
            "",
            "",
            null);

        Assert.True(errors.Count >= 3);
        Assert.Null(observation);
    }

    [Fact]
    public void UpdateContent_ValidContent_ReturnsUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Original content",
            "TestProject",
            "old-key");

        var (errors, updated) = observation!.UpdateContent("Updated content", "new-key");

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal("Updated content", observation.Content);
        Assert.Equal("new-key", observation.TopicKey);
        Assert.Equal(1, observation.RevisionCount);
        Assert.NotNull(observation.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_EmptyContent_ReturnsError()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Original content",
            "TestProject",
            null);

        var (errors, updated) = observation!.UpdateContent("", null);

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateContent_ContentExceedsMaxLength_ReturnsError()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Original content",
            "TestProject",
            null);
        string longContent = new string('a', Observation.Rules.CONTENT_MAX_LENGTH + 1);

        var (errors, updated) = observation!.UpdateContent(longContent, null);

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateContent_TopicKeyExceedsMaxLength_ReturnsError()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Original content",
            "TestProject",
            null);
        string longTopicKey = new string('a', Observation.Rules.TOPIC_KEY_MAX_LENGTH + 1);

        var (errors, updated) = observation!.UpdateContent("Valid content", longTopicKey);

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateContent_NoChanges_ReturnsNotUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Same content",
            "TestProject",
            "same-key");

        var (errors, updated) = observation!.UpdateContent("Same content", "same-key");

        Assert.Empty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateContent_OnlyContentChanged_ReturnsUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Original content",
            "TestProject",
            "same-key");

        var (errors, updated) = observation!.UpdateContent("New content", "same-key");

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal(1, observation.RevisionCount);
    }

    [Fact]
    public void UpdateContent_OnlyTopicKeyChanged_ReturnsUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Same content",
            "TestProject",
            "old-key");

        var (errors, updated) = observation!.UpdateContent("Same content", "new-key");

        Assert.Empty(errors);
        Assert.True(updated);
    }

    [Fact]
    public void UpdateTitle_ValidTitle_ReturnsUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Old title",
            "Content",
            "TestProject",
            null);

        var (errors, updated) = observation!.UpdateTitle("New title");

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal("New title", observation.Title);
        Assert.NotNull(observation.UpdatedAt);
    }

    [Fact]
    public void UpdateTitle_EmptyTitle_ReturnsError()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Old title",
            "Content",
            "TestProject",
            null);

        var (errors, updated) = observation!.UpdateTitle("");

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateTitle_TitleExceedsMaxLength_ReturnsError()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Old title",
            "Content",
            "TestProject",
            null);
        string longTitle = new string('a', Observation.Rules.TITLE_MAX_LENGTH + 1);

        var (errors, updated) = observation!.UpdateTitle(longTitle);

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateTitle_SameTitle_ReturnsNotUpdated()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Same title",
            "Content",
            "TestProject",
            null);

        var (errors, updated) = observation!.UpdateTitle("Same title");

        Assert.Empty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void Observation_CreateWithValidData_HasCorrectDefaults()
    {
        var (_, observation) = Observation.Create(
            _validSessionId,
            ObservationType.Bugfix,
            "Fix bug",
            "Content",
            "TestProject",
            null);

        Assert.NotNull(observation);
        Assert.Equal(0, observation!.RevisionCount);
        Assert.Null(observation.UpdatedAt);
    }
}
