using TemperAI.Installer;

namespace TemperAI.Installer.UnitTests;

public sealed class ClaudeSkillConverterTests
{
    private readonly ClaudeSkillConverter _converter = new();

    private static SkillFlatNameMap BuildMap()
    {
        return SkillFlatNameMap.Build(
        [
            "dotnet-csharp",
            "backend/dotnet/api",
            "backend/dotnet/ef-core/queries",
            "frontend/blazor"
        ]);
    }

    [Fact]
    public void Convert_NestedSkill_SyncsNameToFlatFolderName()
    {
        SkillFlatNameMap map = BuildMap();

        string source = """
            ---
            name: dotnet-api
            description: >
              ASP.NET Core API standards.
              Multiple folded lines preserved.
            requires: [dotnet-csharp]
            ---

            # Body
            """;

        ConvertedSkill result = _converter.Convert(source, "backend/dotnet/api", map);

        Assert.Equal("backend-dotnet-api", result.FlatName);
        Assert.Contains("name: backend-dotnet-api", result.Content);
        Assert.DoesNotContain("name: dotnet-api", result.Content);
        // Folded description and other keys preserved verbatim.
        Assert.Contains("Multiple folded lines preserved.", result.Content);
        Assert.Contains("requires: [dotnet-csharp]", result.Content);
        Assert.Contains("# Body", result.Content);
    }

    [Fact]
    public void Convert_FlatSkill_KeepsSingleSegmentName()
    {
        SkillFlatNameMap map = BuildMap();

        string source = """
            ---
            name: dotnet-csharp
            ---

            # Universal
            """;

        ConvertedSkill result = _converter.Convert(source, "dotnet-csharp", map);

        Assert.Equal("dotnet-csharp", result.FlatName);
        Assert.Contains("name: dotnet-csharp", result.Content);
    }

    [Fact]
    public void Convert_RewritesKnownNestedReferencesInBody()
    {
        SkillFlatNameMap map = BuildMap();

        string source = """
            ---
            name: dotnet-linq
            ---

            For EF Core, load `backend/dotnet/ef-core/queries/SKILL.md`.
            """;

        ConvertedSkill result = _converter.Convert(source, "backend/dotnet/linq", map);

        Assert.Contains("backend-dotnet-ef-core-queries/SKILL.md", result.Content);
        Assert.DoesNotContain("backend/dotnet/ef-core/queries/SKILL.md", result.Content);
    }

    [Fact]
    public void Convert_LeavesUnknownReferencesUntouched()
    {
        SkillFlatNameMap map = BuildMap();

        string source = """
            ---
            name: dotnet-csharp
            ---

            See `some/unknown/path/SKILL.md` for details.
            """;

        ConvertedSkill result = _converter.Convert(source, "dotnet-csharp", map);

        // Not a known skill directory — left as-is rather than blindly rewritten.
        Assert.Contains("some/unknown/path/SKILL.md", result.Content);
    }

    [Fact]
    public void Build_CollidingFlatNames_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SkillFlatNameMap.Build(["backend/dotnet-api", "backend-dotnet/api"]));
    }

    [Fact]
    public void RewriteReferences_PrefersLongestMatch()
    {
        SkillFlatNameMap map = SkillFlatNameMap.Build(
        [
            "backend/dotnet",
            "backend/dotnet/ef-core/queries"
        ]);

        string rewritten = map.RewriteReferences("ref: backend/dotnet/ef-core/queries/SKILL.md");

        Assert.Equal("ref: backend-dotnet-ef-core-queries/SKILL.md", rewritten);
    }

    [Fact]
    public void RewriteReferences_FlattensBareReference()
    {
        SkillFlatNameMap map = BuildMap();

        string rewritten = map.RewriteReferences("Load `backend/dotnet/api` first.");

        Assert.Equal("Load `backend-dotnet-api` first.", rewritten);
    }

    [Fact]
    public void RewriteReferences_DoesNotRewriteShorterPrefixInsideLongerNonSkillPath()
    {
        // Only the deeper path is a real skill; the bare "backend/dotnet/api" prefix must
        // NOT corrupt the longer non-skill "backend/dotnet/api-docs/scalar" reference.
        SkillFlatNameMap map = SkillFlatNameMap.Build(
        [
            "backend/dotnet/api",
            "backend/dotnet/api-docs/scalar"
        ]);

        string rewritten = map.RewriteReferences("see backend/dotnet/api-docs/scalar/SKILL.md");

        Assert.Equal("see backend-dotnet-api-docs-scalar/SKILL.md", rewritten);
    }

    [Fact]
    public void RewriteReferences_LeavesBareUnknownPathUntouched()
    {
        SkillFlatNameMap map = BuildMap();

        string rewritten = map.RewriteReferences("See `Docs/Application/Architecture/backend-config.md`.");

        Assert.Equal("See `Docs/Application/Architecture/backend-config.md`.", rewritten);
    }

    [Fact]
    public void ResolveFlatName_AndRewriteReferences_ProduceIdenticalFlatNames()
    {
        // The same shared map drives both skill folder names and agent reference rewriting,
        // so the flat name resolved for a skill must equal the flat name written into agents.
        SkillFlatNameMap map = BuildMap();

        string skillFlatName = map.ResolveFlatName("backend/dotnet/ef-core/queries");
        string agentRewritten = map.RewriteReferences("`backend/dotnet/ef-core/queries`");

        Assert.Equal("backend-dotnet-ef-core-queries", skillFlatName);
        Assert.Equal($"`{skillFlatName}`", agentRewritten);
    }
}
