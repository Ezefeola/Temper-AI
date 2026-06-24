using System.Text;

namespace TemperAI.Installer;

public sealed class ConvertedAgent
{
    public string FileName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Converts TemperAI agent assets authored in the OpenCode format
/// (<c>name.agent.md</c> with <c>mode</c>/<c>permission</c> frontmatter) into Claude Code
/// agents (<c>~/.claude/agents/&lt;name&gt;.md</c> with a <c>tools</c> list).
///
/// Both <c>mode: subagent</c> specialists and <c>mode: primary</c> orchestrators become
/// agent files: orchestrators carry <c>task: allow</c>, which maps to the <c>Task</c> tool so
/// they can delegate. The orchestrator is then driven as the session agent via
/// <c>claude --agent temper-friday</c> (the Claude Code analog of OpenCode's primary agent),
/// or set as the default through the <c>agent</c> setting in <c>.claude/settings.json</c>.
///
/// Frontmatter is edited via string surgery (not a YAML round-trip) so multi-line
/// folded descriptions (<c>&gt;</c>) are preserved verbatim.
/// </summary>
public sealed class ClaudeAssetConverter
{
    public ConvertedAgent Convert(string sourceContent)
    {
        (string frontmatter, string body) = SplitFrontmatter(sourceContent);

        List<string> frontmatterLines = frontmatter.Length == 0
            ? []
            : frontmatter.Replace("\r\n", "\n").Split('\n').ToList();

        string name = ReadScalar(frontmatterLines, "name");
        PermissionSet permissions = ReadPermissions(frontmatterLines);

        List<string> kept = StripOpenCodeKeys(frontmatterLines);
        kept.Add($"tools: {MapPermissionsToTools(permissions)}");

        return new ConvertedAgent
        {
            FileName = $"{name}.md",
            Content = BuildDocument(kept, body)
        };
    }

    private static (string Frontmatter, string Body) SplitFrontmatter(string content)
    {
        string normalized = content.Replace("\r\n", "\n");

        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (string.Empty, content);
        }

        int closingIndex = normalized.IndexOf("\n---", 3, StringComparison.Ordinal);

        if (closingIndex < 0)
        {
            return (string.Empty, content);
        }

        string frontmatter = normalized[4..closingIndex];

        int bodyStart = closingIndex + "\n---".Length;

        // Skip the rest of the closing delimiter line.
        int newlineAfterDelimiter = normalized.IndexOf('\n', bodyStart);
        string body = newlineAfterDelimiter < 0
            ? string.Empty
            : normalized[(newlineAfterDelimiter + 1)..];

        return (frontmatter, body);
    }

    private static string ReadScalar(List<string> lines, string key)
    {
        string prefix = $"{key}:";

        foreach (string line in lines)
        {
            // Top-level keys only (no leading whitespace).
            if (line.Length == 0 || char.IsWhiteSpace(line[0]))
            {
                continue;
            }

            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[prefix.Length..].Trim();
            }
        }

        return string.Empty;
    }

    private static PermissionSet ReadPermissions(List<string> lines)
    {
        var permissions = new PermissionSet();
        bool insidePermission = false;

        foreach (string line in lines)
        {
            bool isTopLevel = line.Length > 0 && !char.IsWhiteSpace(line[0]);

            if (isTopLevel)
            {
                insidePermission = line.StartsWith("permission:", StringComparison.Ordinal);
                continue;
            }

            if (!insidePermission || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string trimmed = line.Trim();
            int separator = trimmed.IndexOf(':');

            if (separator <= 0)
            {
                continue;
            }

            string permKey = trimmed[..separator].Trim();
            string permValue = trimmed[(separator + 1)..].Trim();
            bool allowed = permValue.Equals("allow", StringComparison.OrdinalIgnoreCase);

            switch (permKey)
            {
                case "read":
                    permissions.Read = allowed;
                    break;
                case "edit":
                    permissions.Edit = allowed;
                    break;
                case "bash":
                    permissions.Bash = allowed;
                    break;
                case "task":
                    permissions.Task = allowed;
                    break;
            }
        }

        return permissions;
    }

    /// <summary>
    /// Removes OpenCode-specific top-level keys (<c>mode</c> and the entire
    /// <c>permission</c> block, including its indented children) while keeping
    /// everything else (notably <c>name</c> and multi-line <c>description</c>).
    /// </summary>
    private static List<string> StripOpenCodeKeys(List<string> lines)
    {
        var kept = new List<string>();
        bool skippingPermissionBlock = false;

        foreach (string line in lines)
        {
            bool isTopLevel = line.Length > 0 && !char.IsWhiteSpace(line[0]);

            if (skippingPermissionBlock)
            {
                // Continue skipping indented children of permission:.
                if (!isTopLevel && line.Length > 0)
                {
                    continue;
                }

                skippingPermissionBlock = false;
            }

            if (isTopLevel && line.StartsWith("mode:", StringComparison.Ordinal))
            {
                continue;
            }

            if (isTopLevel && line.StartsWith("permission:", StringComparison.Ordinal))
            {
                skippingPermissionBlock = true;
                continue;
            }

            kept.Add(line);
        }

        // Drop a trailing blank line left behind by removals.
        while (kept.Count > 0 && kept[^1].Length == 0)
        {
            kept.RemoveAt(kept.Count - 1);
        }

        return kept;
    }

    private static string MapPermissionsToTools(PermissionSet permissions)
    {
        var tools = new List<string>();

        if (permissions.Read)
        {
            tools.Add("Read");
            tools.Add("Glob");
            tools.Add("Grep");
        }

        if (permissions.Edit)
        {
            tools.Add("Edit");
            tools.Add("Write");
        }

        if (permissions.Bash)
        {
            tools.Add("Bash");
        }

        // Orchestrators (task: allow) delegate to specialists via the Task tool.
        if (permissions.Task)
        {
            tools.Add("Task");
        }

        // Always allow loading skills — TemperAI agents load skills on demand.
        tools.Add("Skill");

        return string.Join(", ", tools);
    }

    private static string BuildDocument(List<string> frontmatterLines, string body)
    {
        var builder = new StringBuilder();
        builder.Append("---\n");

        foreach (string line in frontmatterLines)
        {
            builder.Append(line);
            builder.Append('\n');
        }

        builder.Append("---\n");

        if (!string.IsNullOrEmpty(body))
        {
            builder.Append('\n');
            builder.Append(body);
        }

        return builder.ToString();
    }

    private sealed class PermissionSet
    {
        public bool Read { get; set; }
        public bool Edit { get; set; }
        public bool Bash { get; set; }
        public bool Task { get; set; }
    }
}
