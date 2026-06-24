using TemperAI.Core.Configuration;
using TemperAI.Core.Models;

namespace TemperAI.Core.UnitTests.Configuration;

public sealed class AgentTargetsTests
{
    [Fact]
    public void All_IncludesOpenCodeAndClaudeTargets()
    {
        IReadOnlyList<AgentTarget> targets = AgentTargets.All();

        Assert.Contains(targets, t => t.Id == "opencode");
        Assert.Contains(targets, t => t.Id == "claude");
    }

    [Fact]
    public void FindById_Claude_ReturnsClaudeCodeTargetWithExpectedPaths()
    {
        AgentTarget? claude = AgentTargets.FindById("claude");

        Assert.NotNull(claude);
        Assert.Equal("Claude Code", claude!.Name);
        Assert.Equal("claude", claude.Format);
        Assert.Equal("claude", claude.McpConfigFormat);
        Assert.True(claude.Supported);
        Assert.Contains(Path.Combine(".claude", "agents"), claude.AgentsPath);
        Assert.Contains(Path.Combine(".claude", "skills"), claude.SkillsPath);
    }

    [Fact]
    public void FindById_OpenCode_HasOpenCodeFormat()
    {
        AgentTarget? opencode = AgentTargets.FindById("opencode");

        Assert.NotNull(opencode);
        Assert.Equal("opencode", opencode!.Format);
    }
}
