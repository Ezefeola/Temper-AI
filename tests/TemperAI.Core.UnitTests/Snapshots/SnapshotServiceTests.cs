using TemperAI.Core.Snapshots;

namespace TemperAI.Core.UnitTests.Snapshots;

public sealed class SnapshotServiceTests
{
    private readonly SnapshotService _service = new();
    private readonly string _testDirectory;

    public SnapshotServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"temperai-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void CreateSnapshot_NoTrackedFiles_ReturnsFailure()
    {
        SnapshotResult result = _service.CreateSnapshot(_testDirectory, "test");

        Assert.False(result.IsSuccess);
        Assert.Contains("No tracked files found", result.Error);
    }

    [Fact]
    public void CreateSnapshot_WithTrackedFiles_ReturnsSuccess()
    {
        string constitutionPath = Path.Combine(_testDirectory, "constitution.md");
        File.WriteAllText(constitutionPath, "# Test Constitution");

        SnapshotResult result = _service.CreateSnapshot(_testDirectory, "init");

        Assert.True(result.IsSuccess);
        Assert.Contains("constitution.md", result.Files);
        Assert.True(Directory.Exists(result.SnapshotPath));
    }

    [Fact]
    public void CreateSnapshot_CreatesSnapshotWithAllTrackedFiles()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "constitution");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "spec");
        File.WriteAllText(Path.Combine(_testDirectory, "design.md"), "design");
        File.WriteAllText(Path.Combine(_testDirectory, "tasks.md"), "tasks");
        File.WriteAllText(Path.Combine(_testDirectory, "budget.md"), "budget");

        SnapshotResult result = _service.CreateSnapshot(_testDirectory, "full");

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Files.Count);
        Assert.Contains("constitution.md", result.Files);
        Assert.Contains("spec.md", result.Files);
        Assert.Contains("design.md", result.Files);
        Assert.Contains("tasks.md", result.Files);
        Assert.Contains("budget.md", result.Files);
    }

    [Fact]
    public void CreateSnapshot_CreatesDirectoryWithCorrectName()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test");

        SnapshotResult result = _service.CreateSnapshot(_testDirectory, "design");

        Assert.True(result.IsSuccess);
        string snapshotDirName = Path.GetFileName(result.SnapshotPath);
        Assert.Contains("design", snapshotDirName);
    }

    [Fact]
    public void RestoreSnapshot_SnapshotNotFound_ReturnsFailure()
    {
        SnapshotResult result = _service.RestoreSnapshot(_testDirectory, "nonexistent");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public void RestoreSnapshot_RestoresAllFiles()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "original");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "original spec");

        SnapshotResult createResult = _service.CreateSnapshot(_testDirectory, "pre-change");
        Assert.True(createResult.IsSuccess);

        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "modified");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "modified spec");

        SnapshotResult restoreResult = _service.RestoreSnapshot(_testDirectory, createResult.SnapshotPath);

        Assert.True(restoreResult.IsSuccess);
        Assert.Equal("original", File.ReadAllText(Path.Combine(_testDirectory, "constitution.md")));
        Assert.Equal("original spec", File.ReadAllText(Path.Combine(_testDirectory, "spec.md")));
    }

    [Fact]
    public void RestoreSnapshot_PartialRestore_OnlyRestoresExistingFiles()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "original");

        SnapshotResult createResult = _service.CreateSnapshot(_testDirectory, "partial");
        Assert.True(createResult.IsSuccess);

        SnapshotResult restoreResult = _service.RestoreSnapshot(_testDirectory, Path.GetFileName(createResult.SnapshotPath));

        Assert.True(restoreResult.IsSuccess);
        Assert.Contains("constitution.md", restoreResult.Files);
    }

    [Fact]
    public void ListSnapshots_NoSnapshots_ReturnsEmptyList()
    {
        IReadOnlyList<SnapshotInfo> snapshots = _service.ListSnapshots(_testDirectory);

        Assert.Empty(snapshots);
    }

    [Fact]
    public void ListSnapshots_WithSnapshots_ReturnsSortedByDate()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test");

        _service.CreateSnapshot(_testDirectory, "first");
        Thread.Sleep(1100);
        _service.CreateSnapshot(_testDirectory, "second");

        IReadOnlyList<SnapshotInfo> snapshots = _service.ListSnapshots(_testDirectory);

        Assert.Equal(2, snapshots.Count);
        Assert.True(snapshots[0].CreatedAt >= snapshots[1].CreatedAt);
    }

    [Fact]
    public void ListSnapshots_ReturnsCorrectMetadata()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test content");
        File.WriteAllText(Path.Combine(_testDirectory, "spec.md"), "spec content");

        _service.CreateSnapshot(_testDirectory, "design");

        IReadOnlyList<SnapshotInfo> snapshots = _service.ListSnapshots(_testDirectory);

        Assert.Single(snapshots);
        SnapshotInfo info = snapshots[0];
        Assert.Equal("design", info.Phase);
        Assert.Equal(2, info.FileCount);
        Assert.True(info.TotalSizeBytes > 0);
        Assert.NotEqual(DateTime.MinValue, info.CreatedAt);
    }

    [Fact]
    public void DeleteSnapshot_ExistingSnapshot_ReturnsTrue()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test");

        SnapshotResult createResult = _service.CreateSnapshot(_testDirectory, "to-delete");
        Assert.True(createResult.IsSuccess);

        bool deleted = _service.DeleteSnapshot(_testDirectory, Path.GetFileName(createResult.SnapshotPath));

        Assert.True(deleted);
        Assert.False(Directory.Exists(createResult.SnapshotPath));
    }

    [Fact]
    public void DeleteSnapshot_NonExistentSnapshot_ReturnsFalse()
    {
        bool deleted = _service.DeleteSnapshot(_testDirectory, "nonexistent");

        Assert.False(deleted);
    }

    [Fact]
    public void GetLatestSnapshot_NoSnapshots_ReturnsNull()
    {
        SnapshotInfo? latest = _service.GetLatestSnapshot(_testDirectory);

        Assert.Null(latest);
    }

    [Fact]
    public void GetLatestSnapshot_WithSnapshots_ReturnsMostRecent()
    {
        File.WriteAllText(Path.Combine(_testDirectory, "constitution.md"), "test");

        _service.CreateSnapshot(_testDirectory, "first");
        Thread.Sleep(1100);
        _service.CreateSnapshot(_testDirectory, "latest");

        SnapshotInfo? latest = _service.GetLatestSnapshot(_testDirectory);

        Assert.NotNull(latest);
        Assert.True(latest!.CreatedAt >= DateTime.MinValue);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
