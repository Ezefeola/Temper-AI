namespace TemperAI.Core.Models;

public sealed class AgentTarget
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string SkillsPath { get; init; } = string.Empty;
    public string AgentsPath { get; init; } = string.Empty;
    public string ConfigPath { get; init; } = string.Empty;
    public string McpConfigFile { get; init; } = string.Empty;
    public string McpConfigFormat { get; init; } = string.Empty;

    /// <summary>
    /// Asset layout/format for this target: "opencode" or "claude".
    /// Drives how agent assets are written (direct copy vs. converted) and how the
    /// NeuralCore MCP server is configured.
    /// </summary>
    public string Format { get; init; } = string.Empty;

    public bool Supported { get; init; }
}