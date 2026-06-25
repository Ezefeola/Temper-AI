using TemperAI.Installer;

namespace TemperAI.Installer.UnitTests;

public sealed class ClaudeAssetConverterTests
{
    private readonly ClaudeAssetConverter _converter = new();

    [Fact]
    public void Convert_SubagentWithReadAndEdit_ProducesAgentWithTools()
    {
        string source = """
            ---
            name: temper-backend
            description: Backend implementation agent.
            mode: subagent
            permission:
              read: allow
              edit: allow
            ---

            # Body
            Some content.
            """;

        ConvertedAgent result = _converter.Convert(source);

        Assert.Equal("temper-backend.md", result.FileName);
        Assert.Contains("tools: Read, Glob, Grep, Edit, Write, Skill", result.Content);
        Assert.DoesNotContain("mode:", result.Content);
        Assert.DoesNotContain("permission:", result.Content);
        Assert.Contains("# Body", result.Content);
    }

    [Fact]
    public void Convert_ReadOnlySubagent_OmitsEditAndBashTools()
    {
        string source = """
            ---
            name: temper-review
            description: Review agent.
            mode: subagent
            permission:
              read: allow
            ---

            # Review
            """;

        ConvertedAgent result = _converter.Convert(source);

        Assert.Contains("tools: Read, Glob, Grep, Skill", result.Content);
        Assert.DoesNotContain("Write", result.Content);
        Assert.DoesNotContain("Bash", result.Content);
    }

    [Fact]
    public void Convert_PrimaryOrchestrator_ProducesAgentWithTaskToolAndNoBash()
    {
        string source = """
            ---
            name: temper-friday
            description: Orchestrator.
            mode: primary
            permission:
              read: allow
              edit: allow
              bash: deny
              task: allow
              question: allow
            ---

            # FRIDAY
            """;

        ConvertedAgent result = _converter.Convert(source);

        Assert.Equal("temper-friday.md", result.FileName);
        // Orchestrator becomes an agent (usable via `claude --agent`) with Task to delegate.
        Assert.Contains("tools: Read, Glob, Grep, Edit, Write, Task, Skill", result.Content);
        Assert.DoesNotContain("Bash", result.Content);
        Assert.DoesNotContain("mode:", result.Content);
        Assert.DoesNotContain("permission:", result.Content);
        Assert.Contains("# FRIDAY", result.Content);
        Assert.Contains("description: Orchestrator.", result.Content);
    }

    [Fact]
    public void Convert_PreservesMultiLineDescription()
    {
        string source = """
            ---
            name: temper-analyst
            description: >
              Senior Functional Analyst agent.
              Two-phase workflow.
            mode: subagent
            permission:
              read: allow
              edit: allow
            ---

            # Analyst
            """;

        ConvertedAgent result = _converter.Convert(source);

        Assert.Contains("description: >", result.Content);
        Assert.Contains("Senior Functional Analyst agent.", result.Content);
        Assert.Contains("Two-phase workflow.", result.Content);
    }

    [Fact]
    public void Convert_BashAllow_IncludesBashTool()
    {
        string source = """
            ---
            name: temper-devops
            description: DevOps agent.
            mode: subagent
            permission:
              read: allow
              edit: allow
              bash: allow
            ---

            # DevOps
            """;

        ConvertedAgent result = _converter.Convert(source);

        Assert.Contains("Bash", result.Content);
    }

    private static SkillFlatNameMap BuildSkillMap()
    {
        return SkillFlatNameMap.Build(
        [
            "dotnet-csharp",
            "backend/dotnet/api",
            "backend/dotnet/ef-core/queries",
            "workflow/friday/state-schema"
        ]);
    }

    [Fact]
    public void Convert_WithSkillMap_FlattensFullPathReferenceInBody()
    {
        string source = """
            ---
            name: temper-backend
            description: Backend agent.
            mode: subagent
            permission:
              read: allow
              edit: allow
            ---

            # Body
            Load `backend/dotnet/ef-core/queries/SKILL.md` for queries.
            """;

        ConvertedAgent result = _converter.Convert(source, BuildSkillMap());

        Assert.Contains("backend-dotnet-ef-core-queries/SKILL.md", result.Content);
        Assert.DoesNotContain("backend/dotnet/ef-core/queries/SKILL.md", result.Content);
    }

    [Fact]
    public void Convert_WithSkillMap_FlattensBareBacktickReferenceInBody()
    {
        string source = """
            ---
            name: temper-backend
            description: Backend agent.
            mode: subagent
            permission:
              read: allow
              edit: allow
            ---

            # Body
            Load `backend/dotnet/api` before writing controllers.
            """;

        ConvertedAgent result = _converter.Convert(source, BuildSkillMap());

        Assert.Contains("`backend-dotnet-api`", result.Content);
        Assert.DoesNotContain("`backend/dotnet/api`", result.Content);
    }

    [Fact]
    public void Convert_WithSkillMap_FlattensReferenceInFrontmatterDescription()
    {
        string source = """
            ---
            name: temper-friday
            description: Loads workflow/friday/state-schema/SKILL.md on startup.
            mode: primary
            permission:
              read: allow
              task: allow
            ---

            # FRIDAY
            """;

        ConvertedAgent result = _converter.Convert(source, BuildSkillMap());

        Assert.Contains("workflow-friday-state-schema/SKILL.md", result.Content);
        Assert.DoesNotContain("workflow/friday/state-schema/SKILL.md", result.Content);
    }

    [Fact]
    public void Convert_WithSkillMap_LeavesUnknownPathsUntouched()
    {
        string source = """
            ---
            name: temper-backend
            description: Backend agent.
            mode: subagent
            permission:
              read: allow
            ---

            # Body
            See `Docs/Application/Architecture/backend-config.md` and Plan/INDEX.md.
            Also `some/unknown/path/SKILL.md`.
            """;

        ConvertedAgent result = _converter.Convert(source, BuildSkillMap());

        Assert.Contains("Docs/Application/Architecture/backend-config.md", result.Content);
        Assert.Contains("Plan/INDEX.md", result.Content);
        Assert.Contains("some/unknown/path/SKILL.md", result.Content);
    }

    [Fact]
    public void Convert_WithoutSkillMap_KeepsNestedReferences()
    {
        string source = """
            ---
            name: temper-backend
            description: Backend agent.
            mode: subagent
            permission:
              read: allow
            ---

            # Body
            Load `backend/dotnet/api` and `backend/dotnet/ef-core/queries/SKILL.md`.
            """;

        // No map passed — mirrors the OpenCode path, which never rewrites references.
        ConvertedAgent result = _converter.Convert(source);

        Assert.Contains("backend/dotnet/api", result.Content);
        Assert.Contains("backend/dotnet/ef-core/queries/SKILL.md", result.Content);
        Assert.DoesNotContain("backend-dotnet-api", result.Content);
    }
}
