using TemperAI.Core.Incremental;

namespace TemperAI.Core.UnitTests.Incremental;

public sealed class IncrementalUpdateServiceTests
{
    private readonly IncrementalUpdateService _service = new();
    private readonly string _testDirectory;
    private readonly string _snapshotsDirectory;

    public IncrementalUpdateServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"temperai-incremental-{Guid.NewGuid()}");
        _snapshotsDirectory = Path.Combine(_testDirectory, ".snapshots");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void AnalyzeChanges_DirectoryDoesNotExist_ReturnsNoChanges()
    {
        IncrementalResult result = _service.AnalyzeChanges("nonexistent-directory");

        Assert.False(result.RequiresRerun);
    }

    [Fact]
    public void AnalyzeChanges_NoSnapshots_ReturnsNoChanges()
    {
        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.False(result.RequiresRerun);
    }

    [Fact]
    public void AnalyzeChanges_NoFileChanges_ReturnsNoChanges()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_init");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "constitution.md"), "test");
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.False(result.RequiresRerun);
    }

    [Fact]
    public void AnalyzeChanges_ConstitutionChanged_ReturnsAffectedPhases()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_constitution");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "constitution.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "modified");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.Contains("constitution.md", result.ChangedFiles);
        Assert.Contains("temper-constitution", result.AffectedPhases);
    }

    [Fact]
    public void AnalyzeChanges_CascadesCorrectly()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_discover");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "constitution.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "spec.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "design.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "tasks.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "build-plan.md"), "original");

        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "modified");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "design.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "tasks.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "build-plan.md"), "original");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.Contains("temper-constitution", result.AffectedPhases);
        Assert.Contains("temper-spec", result.AffectedPhases);
        Assert.Contains("temper-design", result.AffectedPhases);
        Assert.Contains("temper-tasks", result.AffectedPhases);
        Assert.Contains("temper-plan", result.AffectedPhases);
        Assert.Contains("temper-review", result.AffectedPhases);
        Assert.Contains("temper-docs", result.AffectedPhases);
    }

    [Fact]
    public void AnalyzeChanges_TasksChanged_OnlyAffectsDownstreamPhases()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_tasks");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "constitution.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "spec.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "design.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "tasks.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "build-plan.md"), "original");

        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "same");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "same");
        File.WriteAllText(Path.Combine(_testDirectory, "design.md"), "same");
        File.WriteAllText(Path.Combine(_testDirectory, "tasks.md"), "modified");
        File.WriteAllText(Path.Combine(_testDirectory, "build-plan.md"), "original");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.DoesNotContain("temper-discover", result.AffectedPhases);
        Assert.DoesNotContain("temper-constitution", result.AffectedPhases);
        Assert.DoesNotContain("temper-spec", result.AffectedPhases);
        Assert.DoesNotContain("temper-design", result.AffectedPhases);
        Assert.Contains("temper-tasks", result.AffectedPhases);
        Assert.Contains("temper-plan", result.AffectedPhases);
        Assert.Contains("temper-review", result.AffectedPhases);
        Assert.Contains("temper-docs", result.AffectedPhases);
    }

    [Fact]
    public void GetReRunOrder_ReturnsPhasesInCorrectOrder()
    {
        List<string> affected = ["temper-plan", "temper-review", "temper-docs"];

        IReadOnlyList<PhaseDependency> order = _service.GetReRunOrder(affected);

        Assert.Equal(3, order.Count);
        Assert.Equal("temper-plan", order[0].PhaseName);
        Assert.Equal("temper-review", order[1].PhaseName);
        Assert.Equal("temper-docs", order[2].PhaseName);
    }

    [Fact]
    public void GetReRunOrder_FiltersOnlyAffectedPhases()
    {
        List<string> affected = ["temper-design"];

        IReadOnlyList<PhaseDependency> order = _service.GetReRunOrder(affected);

        Assert.Single(order);
        Assert.Equal("temper-design", order[0].PhaseName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
