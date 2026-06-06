using TemperAI.Installer;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;

namespace TemperAI.Installer.UnitTests;

public sealed class InstallerServiceTests
{
    private readonly InstallerService _service = new();
    private readonly string _testDirectory;

    public InstallerServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"temperai-installer-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void Install_WithLocalAssets_CreatesFiles()
    {
        AgentTarget target = new()
        {
            Id = "test",
            Name = "Test Agent",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Supported = true
        };

        InstallResult result = _service.Install(target, InstallSourceMode.Local);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Install_CreatesDestinationDirectories()
    {
        AgentTarget target = new()
        {
            Id = "test",
            Name = "Test Agent",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        Assert.True(Directory.Exists(Path.Combine(_testDirectory, "skills")));
        Assert.True(Directory.Exists(Path.Combine(_testDirectory, "agents")));
    }

    [Fact]
    public void Install_DryRun_ReturnsDryRunResult()
    {
        InstallerService dryRunService = new(dryRun: true);

        AgentTarget target = new()
        {
            Id = "test",
            Name = "Test Agent",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Supported = true
        };

        InstallResult result = dryRunService.Install(target, InstallSourceMode.Local);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Install_SkipsIgnoredFiles()
    {
        AgentTarget target = new()
        {
            Id = "test",
            Name = "Test Agent",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Supported = true
        };

        InstallResult result = _service.Install(target, InstallSourceMode.Local);

        Assert.DoesNotContain(result.Installed, f => f.Contains("README.md", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Skipped, f => f.Contains("README.md", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Install_SkipsExistingFiles()
    {
        string skillsPath = Path.Combine(_testDirectory, "skills");
        Directory.CreateDirectory(skillsPath);
        string existingFile = Path.Combine(skillsPath, "existing.md");
        File.WriteAllText(existingFile, "existing content");

        AgentTarget target = new()
        {
            Id = "test",
            Name = "Test Agent",
            SkillsPath = skillsPath,
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Supported = true
        };

        InstallResult result = _service.Install(target, InstallSourceMode.Local);

        Assert.DoesNotContain(result.Installed, f => f.Contains("existing.md"));
    }
}
