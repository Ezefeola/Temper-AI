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

    [Fact]
    public void Install_ClaudeTarget_ConvertsAgentsWithTransformedNamesAndTools()
    {
        string agentsPath = Path.Combine(_testDirectory, "agents");

        AgentTarget target = new()
        {
            Id = "claude",
            Name = "Claude Code",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = agentsPath,
            ConfigPath = _testDirectory,
            Format = "claude",
            Supported = true
        };

        InstallResult result = _service.Install(target, InstallSourceMode.Local);

        Assert.True(result.IsSuccess);

        // Specialists land in agents/ as <name>.md (no .agent suffix), with tools and no mode/permission.
        string backendAgent = Path.Combine(agentsPath, "temper-backend.md");
        Assert.True(File.Exists(backendAgent));
        string backendFrontmatter = ExtractFrontmatter(File.ReadAllText(backendAgent));
        Assert.Contains("tools:", backendFrontmatter);
        Assert.DoesNotContain("mode:", backendFrontmatter);
        Assert.DoesNotContain("permission:", backendFrontmatter);

        // The orchestrator is also an agent (usable via `claude --agent`) with the Task tool.
        string fridayAgent = Path.Combine(agentsPath, "temper-friday.md");
        Assert.True(File.Exists(fridayAgent));
        Assert.Contains("Task", ExtractFrontmatter(File.ReadAllText(fridayAgent)));

        // No OpenCode-style .agent.md files were written.
        Assert.False(File.Exists(Path.Combine(agentsPath, "temper-backend.agent.md")));
    }

    [Fact]
    public void Install_ClaudeTarget_CopiesSkillsUnchanged()
    {
        AgentTarget target = new()
        {
            Id = "claude",
            Name = "Claude Code",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Format = "claude",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        Assert.True(Directory.Exists(Path.Combine(_testDirectory, "skills")));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(_testDirectory, "skills"), "SKILL.md", SearchOption.AllDirectories));
    }

    private static string ExtractFrontmatter(string content)
    {
        string normalized = content.Replace("\r\n", "\n");

        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        int closingIndex = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);

        return closingIndex < 0 ? string.Empty : normalized[4..closingIndex];
    }
}
