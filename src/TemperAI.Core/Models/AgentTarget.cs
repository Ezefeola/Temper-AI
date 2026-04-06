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
    public bool Supported { get; init; }
}