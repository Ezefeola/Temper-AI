using TemperAI.NeuralCore.Domain.Entities.Sessions;
using TemperAI.NeuralCore.Domain.Entities.Sessions.Enums;

namespace TemperAI.NeuralCore.Domain.UnitTests.Entities.Sessions;

public sealed class SessionTests
{
    [Fact]
    public void Create_ValidProjectAndDirectory_ReturnsSuccess()
    {
        var (errors, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        Assert.Empty(errors);
        Assert.NotNull(session);
        Assert.Equal("TestProject", session!.Project);
        Assert.Equal("C:\\Projects\\Test", session.Directory);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.NotEqual(default, session.StartedAt);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void Create_EmptyProject_ReturnsError()
    {
        var (errors, session) = Session.Create("", "C:\\Projects\\Test");

        Assert.NotEmpty(errors);
        Assert.Null(session);
        Assert.Contains(errors, e => e.Contains("project", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_WhitespaceProject_ReturnsError()
    {
        var (errors, session) = Session.Create("   ", "C:\\Projects\\Test");

        Assert.NotEmpty(errors);
        Assert.Null(session);
    }

    [Fact]
    public void Create_ProjectExceedsMaxLength_ReturnsError()
    {
        string longProject = new string('a', Session.Rules.PROJECT_MAX_LENGTH + 1);

        var (errors, session) = Session.Create(longProject, "C:\\Projects\\Test");

        Assert.NotEmpty(errors);
        Assert.Null(session);
        Assert.Contains(errors, e => e.Contains("exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_EmptyDirectory_ReturnsError()
    {
        var (errors, session) = Session.Create("TestProject", "");

        Assert.NotEmpty(errors);
        Assert.Null(session);
        Assert.Contains(errors, e => e.Contains("directory", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_DirectoryExceedsMaxLength_ReturnsError()
    {
        string longDirectory = new string('a', Session.Rules.DIRECTORY_MAX_LENGTH + 1);

        var (errors, session) = Session.Create("TestProject", longDirectory);

        Assert.NotEmpty(errors);
        Assert.Null(session);
    }

    [Fact]
    public void Create_BothInvalid_ReturnsMultipleErrors()
    {
        var (errors, session) = Session.Create("", "");

        Assert.True(errors.Count >= 2);
        Assert.Null(session);
    }

    [Fact]
    public void Create_ExactMaxLengthProject_Succeeds()
    {
        string exactProject = new string('a', Session.Rules.PROJECT_MAX_LENGTH);

        var (errors, session) = Session.Create(exactProject, "C:\\Projects\\Test");

        Assert.Empty(errors);
        Assert.NotNull(session);
    }

    [Fact]
    public void UpdateSummary_ValidSummary_ReturnsUpdated()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        var (errors, updated) = session!.UpdateSummary("This is a test summary");

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal("This is a test summary", session.Summary);
    }

    [Fact]
    public void UpdateSummary_EmptySummary_ReturnsError()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        var (errors, updated) = session!.UpdateSummary("");

        Assert.NotEmpty(errors);
        Assert.False(updated);
        Assert.Contains(errors, e => e.Contains("summary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UpdateSummary_ExceedsMaxLength_ReturnsError()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");
        string longSummary = new string('a', Session.Rules.SUMMARY_MAX_LENGTH + 1);

        var (errors, updated) = session!.UpdateSummary(longSummary);

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void UpdateSummary_SameSummary_ReturnsNotUpdated()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");
        session!.UpdateSummary("Same summary");

        var (errors, updated) = session.UpdateSummary("Same summary");

        Assert.Empty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void Complete_ActiveSession_ReturnsCompleted()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        var (errors, updated) = session!.Complete();

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Complete_NonActiveSession_ReturnsError()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");
        session!.Complete();

        var (errors, updated) = session.Complete();

        Assert.NotEmpty(errors);
        Assert.False(updated);
        Assert.Contains(errors, e => e.Contains("active", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Abandon_ActiveSession_ReturnsAbandoned()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        var (errors, updated) = session!.Abandon();

        Assert.Empty(errors);
        Assert.True(updated);
        Assert.Equal(SessionStatus.Abandoned, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public void Abandon_NonActiveSession_ReturnsError()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");
        session!.Abandon();

        var (errors, updated) = session.Abandon();

        Assert.NotEmpty(errors);
        Assert.False(updated);
    }

    [Fact]
    public void Session_CreateWithValidData_HasCorrectDefaults()
    {
        var (_, session) = Session.Create("TestProject", "C:\\Projects\\Test");

        Assert.NotNull(session);
        Assert.Equal(string.Empty, session!.Summary);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Null(session.EndedAt);
    }
}
