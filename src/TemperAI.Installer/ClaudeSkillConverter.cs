using System.Text;

namespace TemperAI.Installer;

public sealed class ConvertedSkill
{
    /// <summary>
    /// Flat folder name the skill is installed under, e.g. <c>backend-dotnet-api</c>.
    /// </summary>
    public string FlatName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Maps a skill's source relative directory (the segments between the skills root and
/// its <c>SKILL.md</c>) to the flat folder name Claude Code requires.
/// </summary>
public sealed class SkillFlatNameMap
{
    private const string SkillFileName = "SKILL.md";

    // Relative directory (forward-slash, lowercase) -> flat folder name.
    private readonly Dictionary<string, string> _byRelativeDirectory;

    private SkillFlatNameMap(Dictionary<string, string> byRelativeDirectory)
    {
        _byRelativeDirectory = byRelativeDirectory;
    }

    /// <summary>
    /// Builds the flat-name map from the relative directories of every discovered skill.
    /// Each directory is flattened by joining its segments with <c>-</c>; already-flat
    /// skills keep their single-segment name. Throws if two distinct source directories
    /// would collapse onto the same flat name.
    /// </summary>
    public static SkillFlatNameMap Build(IEnumerable<string> relativeDirectories)
    {
        var byRelativeDirectory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var claimedFlatNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string relativeDirectory in relativeDirectories)
        {
            string normalized = NormalizeDirectory(relativeDirectory);

            if (normalized.Length == 0 || byRelativeDirectory.ContainsKey(normalized))
            {
                continue;
            }

            string flatName = Flatten(normalized);

            if (claimedFlatNames.TryGetValue(flatName, out string? owner) &&
                !owner.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Colisión de nombre de skill aplanado '{flatName}': '{owner}' y '{normalized}'.");
            }

            byRelativeDirectory[normalized] = flatName;
            claimedFlatNames[flatName] = normalized;
        }

        return new SkillFlatNameMap(byRelativeDirectory);
    }

    /// <summary>
    /// Resolves the flat folder name for the given source relative directory.
    /// </summary>
    public string ResolveFlatName(string relativeDirectory)
    {
        string normalized = NormalizeDirectory(relativeDirectory);

        if (_byRelativeDirectory.TryGetValue(normalized, out string? flatName))
        {
            return flatName;
        }

        return Flatten(normalized);
    }

    /// <summary>
    /// Rewrites nested skill references found in <paramref name="content"/> to their flat
    /// folder name, but only when the referenced directory corresponds to a real installed
    /// skill. Two reference forms are rewritten:
    /// <list type="bullet">
    /// <item>full paths — <c>seg/seg/.../SKILL.md</c> → <c>seg-seg-.../SKILL.md</c>;</item>
    /// <item>bare references — <c>seg/seg/...</c> (e.g. inside backticks) → <c>seg-seg-...</c>.</item>
    /// </list>
    /// References to already-flat skills and unknown paths are left untouched. Matching is
    /// longest-directory-first and boundary-aware, so a shorter skill directory is never
    /// rewritten inside a longer (possibly non-skill) path that merely shares its prefix.
    /// </summary>
    public string RewriteReferences(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        string result = content;

        // Longest source directories first so deeper paths are replaced before any
        // shorter prefix could match a substring of them.
        foreach (KeyValuePair<string, string> entry in _byRelativeDirectory
                     .OrderByDescending(pair => pair.Key.Length))
        {
            string relativeDirectory = entry.Key;
            string flatName = entry.Value;

            // Only multi-segment references change; flat skills already map to themselves.
            if (!relativeDirectory.Contains('/'))
            {
                continue;
            }

            result = ReplaceDirectoryReferences(result, relativeDirectory, flatName);
        }

        return result;
    }

    /// <summary>
    /// Replaces every whole-token occurrence of <paramref name="relativeDirectory"/> in
    /// <paramref name="content"/> with <paramref name="flatName"/>, covering both the
    /// <c>.../SKILL.md</c> full-path form and the bare form. A match is accepted only when it
    /// is bounded as a complete path token: preceded by a non-name character and followed by
    /// <c>/SKILL.md</c> or a non-continuation boundary. This prevents a shorter skill
    /// directory from being rewritten inside a longer path that shares its prefix.
    /// </summary>
    private static string ReplaceDirectoryReferences(string content, string relativeDirectory, string flatName)
    {
        StringBuilder builder = new(content.Length);
        int searchStart = 0;

        while (searchStart < content.Length)
        {
            int matchIndex = content.IndexOf(relativeDirectory, searchStart, StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                builder.Append(content, searchStart, content.Length - searchStart);
                break;
            }

            int matchEnd = matchIndex + relativeDirectory.Length;

            if (HasLeadingBoundary(content, matchIndex) && TryMatchTrailing(content, matchEnd, out int consumedEnd))
            {
                builder.Append(content, searchStart, matchIndex - searchStart);
                builder.Append(flatName);

                // Re-append the trailing "/SKILL.md" (if any) verbatim so only the
                // directory segment is flattened.
                builder.Append(content, matchEnd, consumedEnd - matchEnd);
                searchStart = consumedEnd;
            }
            else
            {
                // Not a whole-token match — keep the text up to and including this
                // occurrence start, then continue searching past it.
                builder.Append(content, searchStart, matchIndex + 1 - searchStart);
                searchStart = matchIndex + 1;
            }
        }

        return builder.ToString();
    }

    private static bool HasLeadingBoundary(string content, int matchIndex)
    {
        if (matchIndex == 0)
        {
            return true;
        }

        return !IsNameCharacter(content[matchIndex - 1]);
    }

    /// <summary>
    /// Validates the text immediately after a candidate directory match. Accepts the full
    /// <c>/SKILL.md</c> suffix (which is consumed and re-emitted verbatim) or a bare
    /// reference that ends at a non-continuation boundary. Rejects a deeper path
    /// (<c>relativeDirectory</c> followed by another segment), which must be handled by a
    /// longer key instead.
    /// </summary>
    private static bool TryMatchTrailing(string content, int matchEnd, out int consumedEnd)
    {
        string skillSuffix = $"/{SkillFileName}";

        if (matchEnd + skillSuffix.Length <= content.Length &&
            string.Compare(content, matchEnd, skillSuffix, 0, skillSuffix.Length, StringComparison.OrdinalIgnoreCase) == 0)
        {
            consumedEnd = matchEnd + skillSuffix.Length;
            return true;
        }

        consumedEnd = matchEnd;

        // End of content, or a boundary that does not continue the path token.
        if (matchEnd >= content.Length)
        {
            return true;
        }

        char next = content[matchEnd];

        // A '/' or name character means a deeper/different path token — not this skill.
        return next != '/' && !IsNameCharacter(next);
    }

    private static bool IsNameCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '-' || value == '_' || value == '.';
    }

    private static string NormalizeDirectory(string relativeDirectory)
    {
        return relativeDirectory
            .Replace('\\', '/')
            .Trim('/');
    }

    private static string Flatten(string normalizedDirectory)
    {
        return normalizedDirectory.Replace('/', '-');
    }
}

/// <summary>
/// Converts TemperAI skill assets — authored in a deep, OpenCode-style nested layout
/// (<c>backend/dotnet/api/SKILL.md</c>) — into the flat, one-level layout Claude Code
/// requires for discovery (<c>&lt;skills-root&gt;/&lt;name&gt;/SKILL.md</c>).
///
/// Each nested directory is collapsed into a single flat folder name (segments joined
/// with <c>-</c>), the frontmatter <c>name:</c> is synced to that flat name so it matches
/// the directory Claude derives the skill name from, and nested cross-references in the
/// body/description are rewritten to the flat names so the guidance stays valid.
///
/// Frontmatter is edited via string surgery (not a YAML round-trip) so multi-line folded
/// descriptions (<c>&gt;</c>) are preserved verbatim — mirroring <see cref="ClaudeAssetConverter"/>.
/// </summary>
public sealed class ClaudeSkillConverter
{
    private const string NameKey = "name";

    public ConvertedSkill Convert(string sourceContent, string relativeDirectory, SkillFlatNameMap flatNameMap)
    {
        string flatName = flatNameMap.ResolveFlatName(relativeDirectory);

        (string frontmatter, string body) = SplitFrontmatter(sourceContent);

        List<string> frontmatterLines = frontmatter.Length == 0
            ? []
            : frontmatter.Replace("\r\n", "\n").Split('\n').ToList();

        List<string> syncedFrontmatter = SyncName(frontmatterLines, flatName);

        // Rewrite nested skill references in the frontmatter (notably the folded
        // description: block) AFTER name-sync, so the already-flat name: value — which
        // contains no '/' — is never touched by the rewrite. Mirrors ClaudeAssetConverter,
        // reusing the same shared SkillFlatNameMap.RewriteReferences so skills and agents
        // emit identical flat names.
        List<string> rewrittenFrontmatter = RewriteFrontmatterReferences(syncedFrontmatter, flatNameMap);

        string content = frontmatterLines.Count == 0
            ? flatNameMap.RewriteReferences(sourceContent)
            : BuildDocument(rewrittenFrontmatter, flatNameMap.RewriteReferences(body));

        return new ConvertedSkill
        {
            FlatName = flatName,
            Content = content
        };
    }

    /// <summary>
    /// Rewrites nested skill references on each frontmatter line in place (preserving line
    /// order, the folded <c>&gt;</c> structure, and indentation verbatim), using the shared
    /// <see cref="SkillFlatNameMap.RewriteReferences"/>. The synced <c>name:</c> line already
    /// holds the flat name — which has no <c>/</c> — so it is left untouched by the rewrite.
    /// </summary>
    private static List<string> RewriteFrontmatterReferences(List<string> lines, SkillFlatNameMap flatNameMap)
    {
        List<string> rewritten = new(lines.Count);

        foreach (string line in lines)
        {
            rewritten.Add(flatNameMap.RewriteReferences(line));
        }

        return rewritten;
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

    /// <summary>
    /// Replaces the top-level <c>name:</c> scalar with the flat folder name, preserving
    /// every other line (including multi-line folded <c>description:</c> blocks) verbatim.
    /// If no <c>name:</c> key is present, one is prepended.
    /// </summary>
    private static List<string> SyncName(List<string> lines, string flatName)
    {
        string prefix = $"{NameKey}:";
        var synced = new List<string>(lines.Count);
        bool replaced = false;

        foreach (string line in lines)
        {
            bool isTopLevel = line.Length > 0 && !char.IsWhiteSpace(line[0]);

            if (!replaced && isTopLevel && line.StartsWith(prefix, StringComparison.Ordinal))
            {
                synced.Add($"{NameKey}: {flatName}");
                replaced = true;
                continue;
            }

            synced.Add(line);
        }

        if (!replaced)
        {
            synced.Insert(0, $"{NameKey}: {flatName}");
        }

        return synced;
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
}
