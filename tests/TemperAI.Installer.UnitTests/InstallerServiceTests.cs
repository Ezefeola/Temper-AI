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
    public void Install_ClaudeTarget_FlattensSkillsToOneLevel()
    {
        string skillsPath = Path.Combine(_testDirectory, "skills");

        AgentTarget target = new()
        {
            Id = "claude",
            Name = "Claude Code",
            SkillsPath = skillsPath,
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Format = "claude",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        Assert.True(Directory.Exists(skillsPath));

        // Every SKILL.md must land exactly one level deep: <skills>/<flat-name>/SKILL.md.
        string[] skillFiles = Directory.GetFiles(skillsPath, "SKILL.md", SearchOption.AllDirectories);
        Assert.NotEmpty(skillFiles);

        foreach (string skillFile in skillFiles)
        {
            string flatFolder = Path.GetDirectoryName(skillFile)!;
            Assert.Equal(skillsPath, Path.GetDirectoryName(flatFolder));
        }

        // A previously nested skill is reachable under its flattened folder name.
        string flattenedApi = Path.Combine(skillsPath, "backend-dotnet-api", "SKILL.md");
        Assert.True(File.Exists(flattenedApi));

        // Frontmatter name is synced to the flat folder name.
        Assert.Contains("name: backend-dotnet-api", File.ReadAllText(flattenedApi));

        // An already-flat skill keeps its single-segment name.
        Assert.True(File.Exists(Path.Combine(skillsPath, "dotnet-csharp", "SKILL.md")));
    }

    [Fact]
    public void Install_ClaudeTarget_RewritesNestedSkillCrossReferences()
    {
        string skillsPath = Path.Combine(_testDirectory, "skills");

        AgentTarget target = new()
        {
            Id = "claude",
            Name = "Claude Code",
            SkillsPath = skillsPath,
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Format = "claude",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        // dotnet-linq references backend/dotnet/ef-core/queries/SKILL.md in its body — it must
        // be rewritten to the flat name so the guidance stays valid under Claude discovery.
        string linq = File.ReadAllText(Path.Combine(skillsPath, "backend-dotnet-linq", "SKILL.md"));
        Assert.Contains("backend-dotnet-ef-core-queries/SKILL.md", linq);
        Assert.DoesNotContain("backend/dotnet/ef-core/queries/SKILL.md", linq);
    }

    [Fact]
    public void Install_ClaudeTarget_RewritesNestedSkillReferencesInFrontmatterDescription()
    {
        string skillsPath = Path.Combine(_testDirectory, "skills");

        AgentTarget target = new()
        {
            Id = "claude",
            Name = "Claude Code",
            SkillsPath = skillsPath,
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Format = "claude",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        // dotnet-linq references backend/dotnet/ef-core/queries/SKILL.md inside its folded
        // `description:` frontmatter block. That reference must be flattened just like the body
        // ones, otherwise the guidance under Claude points at a non-existent nested path.
        string linq = File.ReadAllText(Path.Combine(skillsPath, "backend-dotnet-linq", "SKILL.md"));
        string frontmatter = ExtractFrontmatter(linq);

        Assert.Contains("backend-dotnet-ef-core-queries/SKILL.md", frontmatter);
        Assert.DoesNotContain("backend/dotnet/ef-core/queries/SKILL.md", frontmatter);

        // The name: must stay synced to the flat folder name (not corrupted by the rewrite),
        // and the folded `description: >` block must remain intact.
        Assert.Contains("name: backend-dotnet-linq", frontmatter);
        Assert.Contains("description: >", frontmatter);
    }

    [Fact]
    public void Install_OpenCodeTarget_KeepsSkillsNested()
    {
        string skillsPath = Path.Combine(_testDirectory, "skills");

        AgentTarget target = new()
        {
            Id = "opencode",
            Name = "OpenCode",
            SkillsPath = skillsPath,
            AgentsPath = Path.Combine(_testDirectory, "agents"),
            ConfigPath = _testDirectory,
            Format = "opencode",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        // OpenCode preserves the deep nested layout unchanged.
        Assert.True(File.Exists(Path.Combine(skillsPath, "backend", "dotnet", "api", "SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(skillsPath, "backend-dotnet-api")));
    }

    [Fact]
    public void Install_ClaudeTarget_RewritesNestedSkillReferencesInAgents()
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

        _service.Install(target, InstallSourceMode.Local);

        // temper-backend loads many nested skills (full-path form, e.g.
        // backend/dotnet/api/SKILL.md). Under Claude they must point at the same flat names
        // the skills are installed under.
        string backendAgent = File.ReadAllText(Path.Combine(agentsPath, "temper-backend.md"));
        Assert.Contains("backend-dotnet-api", backendAgent);
        Assert.DoesNotContain("backend/dotnet/api/SKILL.md", backendAgent);

        // Non-skill paths the agent mentions must be left untouched.
        Assert.Contains("Docs/Application/Architecture/backend-config.md", backendAgent);

        // temper-review uses the bare backtick form (`backend/dotnet/api`); it must flatten too.
        string reviewAgent = File.ReadAllText(Path.Combine(agentsPath, "temper-review.md"));
        Assert.Contains("`backend-dotnet-api`", reviewAgent);
        Assert.DoesNotContain("`backend/dotnet/api`", reviewAgent);
    }

    [Fact]
    public void Install_OpenCodeTarget_KeepsNestedSkillReferencesInAgents()
    {
        string agentsPath = Path.Combine(_testDirectory, "agents");

        AgentTarget target = new()
        {
            Id = "opencode",
            Name = "OpenCode",
            SkillsPath = Path.Combine(_testDirectory, "skills"),
            AgentsPath = agentsPath,
            ConfigPath = _testDirectory,
            Format = "opencode",
            Supported = true
        };

        _service.Install(target, InstallSourceMode.Local);

        // OpenCode copies agents verbatim (.agent.md), so nested references are preserved.
        string backendAgent = File.ReadAllText(Path.Combine(agentsPath, "temper-backend.agent.md"));
        Assert.Contains("backend/dotnet/api", backendAgent);
        Assert.DoesNotContain("backend-dotnet-api", backendAgent);
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
