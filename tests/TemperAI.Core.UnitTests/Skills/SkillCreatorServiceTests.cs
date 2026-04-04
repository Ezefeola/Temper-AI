using TemperAI.Core.Skills;

namespace TemperAI.Core.UnitTests.Skills;

public sealed class SkillCreatorServiceTests
{
    private readonly SkillCreatorService _service = new();
    private readonly string _testDirectory;

    public SkillCreatorServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"temperai-skills-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void CreateSkill_CreatesAllFiles()
    {
        SkillMetadata metadata = new()
        {
            Name = "test-skill",
            Category = "backend",
            Author = "test-author",
            Description = "A test skill",
            Version = "1.0.0"
        };

        SkillResult result = _service.CreateSkill(_testDirectory, "backend", "test-skill", metadata);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.CreatedFiles.Count);
        Assert.Contains(result.CreatedFiles, f => f.EndsWith("SKILL.md"));
        Assert.Contains(result.CreatedFiles, f => f.EndsWith("metadata.json"));
        Assert.Contains(result.CreatedFiles, f => f.EndsWith("README.md"));
    }

    [Fact]
    public void CreateSkill_CreatesCorrectDirectoryStructure()
    {
        SkillMetadata metadata = new()
        {
            Name = "my-skill",
            Category = "custom",
            Author = "me",
            Description = "test"
        };

        SkillResult result = _service.CreateSkill(_testDirectory, "custom", "my-skill", metadata);

        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(result.SkillPath));
        Assert.True(File.Exists(Path.Combine(result.SkillPath, "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(result.SkillPath, "metadata.json")));
        Assert.True(File.Exists(Path.Combine(result.SkillPath, "README.md")));
    }

    [Fact]
    public void CreateSkill_SkillMdContainsMetadata()
    {
        SkillMetadata metadata = new()
        {
            Name = "my-skill",
            Category = "backend",
            Author = "test-author",
            Description = "Test description",
            Version = "2.0.0"
        };

        SkillResult result = _service.CreateSkill(_testDirectory, "backend", "my-skill", metadata);
        Assert.True(result.IsSuccess);

        string skillMdPath = result.CreatedFiles.First(f => f.EndsWith("SKILL.md"));
        string content = File.ReadAllText(skillMdPath);

        Assert.Contains("my-skill", content);
        Assert.Contains("test-author", content);
        Assert.Contains("Test description", content);
        Assert.Contains("2.0.0", content);
        Assert.Contains("backend", content);
    }

    [Fact]
    public void CreateSkill_MetadataJsonIsValid()
    {
        SkillMetadata metadata = new()
        {
            Name = "json-test",
            Category = "frontend",
            Author = "tester",
            Description = "JSON test",
            Version = "1.2.3",
            Dependencies = ["dep1", "dep2"],
            Tags = ["tag1", "tag2"]
        };

        SkillResult result = _service.CreateSkill(_testDirectory, "frontend", "json-test", metadata);
        Assert.True(result.IsSuccess);

        string metadataPath = result.CreatedFiles.First(f => f.EndsWith("metadata.json"));
        string content = File.ReadAllText(metadataPath);

        Assert.Contains("json-test", content);
        Assert.Contains("frontend", content);
        Assert.Contains("tester", content);
        Assert.Contains("dep1", content);
        Assert.Contains("tag1", content);
    }

    [Fact]
    public void InstallSkill_InvalidSourcePath_ReturnsFailure()
    {
        SkillResult result = _service.InstallSkill(_testDirectory, "nonexistent-path");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public void InstallSkill_NoMetadataJson_ReturnsFailure()
    {
        string sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "SKILL.md"), "test");

        SkillResult result = _service.InstallSkill(_testDirectory, sourceDir);

        Assert.False(result.IsSuccess);
        Assert.Contains("metadata.json", result.Error);
    }

    [Fact]
    public void InstallSkill_ValidSkill_InstallsCorrectly()
    {
        SkillMetadata metadata = new()
        {
            Name = "install-test",
            Category = "backend",
            Author = "installer",
            Description = "Install test"
        };

        string sourceDir = Path.Combine(_testDirectory, "source");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "SKILL.md"), "skill content");
        File.WriteAllText(Path.Combine(sourceDir, "metadata.json"), "{\"name\":\"install-test\",\"category\":\"backend\",\"author\":\"installer\",\"description\":\"Install test\"}");

        string skillsDir = Path.Combine(_testDirectory, "skills");
        SkillResult result = _service.InstallSkill(skillsDir, sourceDir);

        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(Path.Combine(skillsDir, "backend", "install-test")));
        Assert.True(File.Exists(Path.Combine(skillsDir, "backend", "install-test", "SKILL.md")));
    }

    [Fact]
    public void DiscoverSkills_NoSkills_ReturnsEmptyList()
    {
        IReadOnlyList<SkillInfo> skills = _service.DiscoverSkills(_testDirectory);

        Assert.Empty(skills);
    }

    [Fact]
    public void DiscoverSkills_WithSkills_ReturnsAllSkills()
    {
        SkillMetadata metadata1 = new()
        {
            Name = "skill-one",
            Category = "backend",
            Author = "author1",
            Description = "First skill"
        };

        SkillMetadata metadata2 = new()
        {
            Name = "skill-two",
            Category = "frontend",
            Author = "author2",
            Description = "Second skill"
        };

        _service.CreateSkill(_testDirectory, "backend", "skill-one", metadata1);
        _service.CreateSkill(_testDirectory, "frontend", "skill-two", metadata2);

        IReadOnlyList<SkillInfo> skills = _service.DiscoverSkills(Path.Combine(_testDirectory, "skills"));

        Assert.Equal(2, skills.Count);
    }

    [Fact]
    public void DiscoverSkills_ReturnsCorrectMetadata()
    {
        SkillMetadata metadata = new()
        {
            Name = "metadata-test",
            Category = "backend",
            Author = "test-author",
            Description = "Metadata test description",
            Version = "3.0.0",
            Tags = ["test", "backend"]
        };

        _service.CreateSkill(_testDirectory, "backend", "metadata-test", metadata);

        IReadOnlyList<SkillInfo> skills = _service.DiscoverSkills(Path.Combine(_testDirectory, "skills"));

        Assert.Single(skills);
        SkillInfo info = skills[0];
        Assert.Equal("metadata-test", info.Name);
        Assert.Equal("backend", info.Category);
        Assert.Equal("test-author", info.Author);
        Assert.Equal("Metadata test description", info.Description);
        Assert.Equal("3.0.0", info.Version);
        Assert.True(info.FileCount > 0);
        Assert.True(info.TotalSizeBytes > 0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
