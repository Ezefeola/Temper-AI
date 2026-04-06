using TemperAI.Core.Models;

namespace TemperAI.Core.Configuration;

public static class AgentTargets
{
    public static IReadOnlyList<AgentTarget> All()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);

        return new List<AgentTarget>
        {
            new()
            {
                Id = "copilot",
                Name = "GitHub Copilot CLI",
                SkillsPath = Path.Combine(homeDirectory, ".copilot", "skills"),
                AgentsPath = Path.Combine(homeDirectory, ".copilot", "agents"),
                ConfigPath = Path.Combine(homeDirectory, ".copilot"),
                McpConfigFile = Path.Combine(homeDirectory, ".github", "copilot", "mcp.json"),
                McpConfigFormat = "copilot",
                Supported = true
            },
            new()
            {
                Id = "claude",
                Name = "Claude Code",
                SkillsPath = Path.Combine(homeDirectory, ".claude", "skills"),
                AgentsPath = Path.Combine(homeDirectory, ".claude", "agents"),
                ConfigPath = Path.Combine(homeDirectory, ".claude"),
                McpConfigFile = Path.Combine(homeDirectory, ".claude", "mcp.json"),
                McpConfigFormat = "claude",
                Supported = true
            },
            new()
            {
                Id = "opencode",
                Name = "OpenCode",
                SkillsPath = Path.Combine(homeDirectory, ".config", "opencode", "skills"),
                AgentsPath = Path.Combine(homeDirectory, ".config", "opencode", "agent"),
                ConfigPath = Path.Combine(homeDirectory, ".config", "opencode"),
                McpConfigFile = Path.Combine(homeDirectory, ".config", "opencode", "opencode.json"),
                McpConfigFormat = "opencode",
                Supported = true
            }
        }.AsReadOnly();
    }

    public static AgentTarget? FindById(string id)
    {
        return All().FirstOrDefault(target =>
            target.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<AgentTarget> Supported()
    {
        return All()
            .Where(target => target.Supported)
            .ToList()
            .AsReadOnly();
    }
}