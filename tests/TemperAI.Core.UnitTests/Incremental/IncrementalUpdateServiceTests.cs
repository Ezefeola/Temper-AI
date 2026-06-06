using TemperAI.Core.Incremental;

namespace TemperAI.Core.UnitTests.Incremental;

public sealed class IncrementalUpdateServiceTests : IDisposable
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
        File.WriteAllText(Path.Combine(snapshotDir, "prd.md"), "test");
        File.WriteAllText(Path.Combine(_testDirectory, "prd.md"), "test");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.False(result.RequiresRerun);
    }

    [Fact]
    public void AnalyzeChanges_PrdChanged_ReturnsAffectedPhases()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_prd");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "prd.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "prd.md"), "modified");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.Contains("prd.md", result.ChangedFiles);
        Assert.Contains("temper-analyst-prd", result.AffectedPhases);
    }

    [Fact]
    public void AnalyzeChanges_CascadesCorrectly()
    {
        string snapshotDir = Path.Combine(_snapshotsDirectory, "20260404-120000_analyst");
        Directory.CreateDirectory(snapshotDir);
        File.WriteAllText(Path.Combine(snapshotDir, "prd.md"), "original");
        Directory.CreateDirectory(Path.Combine(snapshotDir, "specs"));
        File.WriteAllText(Path.Combine(snapshotDir, "specs/INDEX.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "prd.md"), "modified");

        Directory.CreateDirectory(Path.Combine(_testDirectory, "specs"));
        File.WriteAllText(Path.Combine(_testDirectory, "specs/INDEX.md"), "original");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.Contains("temper-analyst-prd", result.AffectedPhases);
        Assert.Contains("temper-analyst-spec", result.AffectedPhases);
        Assert.Contains("temper-architect", result.AffectedPhases);
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
        Directory.CreateDirectory(Path.Combine(snapshotDir, "specs"));
        Directory.CreateDirectory(Path.Combine(snapshotDir, "tasks"));
        File.WriteAllText(Path.Combine(snapshotDir, "prd.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "specs/INDEX.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "backend-config.md"), "same");
        File.WriteAllText(Path.Combine(snapshotDir, "tasks/INDEX.md"), "original");
        File.WriteAllText(Path.Combine(snapshotDir, "build-plan.md"), "original");

        File.WriteAllText(Path.Combine(_testDirectory, "prd.md"), "same");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "specs"));
        Directory.CreateDirectory(Path.Combine(_testDirectory, "tasks"));
        File.WriteAllText(Path.Combine(_testDirectory, "specs/INDEX.md"), "same");
        File.WriteAllText(Path.Combine(_testDirectory, "backend-config.md"), "same");

        File.WriteAllText(Path.Combine(_testDirectory, "tasks/INDEX.md"), "modified");
        File.WriteAllText(Path.Combine(_testDirectory, "build-plan.md"), "original");

        IncrementalResult result = _service.AnalyzeChanges(_testDirectory);

        Assert.True(result.RequiresRerun);
        Assert.DoesNotContain("temper-analyst-prd", result.AffectedPhases);
        Assert.DoesNotContain("temper-analyst-spec", result.AffectedPhases);
        Assert.DoesNotContain("temper-architect", result.AffectedPhases);
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
        List<string> affected = ["temper-architect"];

        IReadOnlyList<PhaseDependency> order = _service.GetReRunOrder(affected);

        Assert.Single(order);
        Assert.Equal("temper-architect", order[0].PhaseName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
