namespace TemperAI.Installer;

/// <summary>
/// Translates the global <c>--local</c> flag into the existing <c>--source local</c>
/// behavior. <c>--local</c> is sugar so developers can install/update from the local
/// repo assets without typing <c>--source local</c> every time. Pure and testable so the
/// arg-rewriting logic stays out of <c>Program.cs</c>'s top-level statements.
/// </summary>
public static class LocalModeArguments
{
    public const string LocalFlag = "--local";

    private const string SourceOption = "--source";

    private static readonly HashSet<string> _sourceAwareCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "install",
        "update"
    };

    /// <summary>
    /// Strips every <c>--local</c> occurrence from <paramref name="args"/>. When present and
    /// the first remaining argument is a source-aware command (install/update) without an
    /// explicit <c>--source</c>, injects <c>--source local</c>.
    /// </summary>
    public static (bool LocalMode, string[] Args) Process(string[] args)
    {
        bool localMode = args.Any(arg => arg.Equals(LocalFlag, StringComparison.OrdinalIgnoreCase));

        if (!localMode)
        {
            return (false, args);
        }

        List<string> rewritten = args
            .Where(arg => !arg.Equals(LocalFlag, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (rewritten.Count > 0
            && _sourceAwareCommands.Contains(rewritten[0])
            && !rewritten.Any(arg => arg.Equals(SourceOption, StringComparison.OrdinalIgnoreCase)))
        {
            rewritten.Add(SourceOption);
            rewritten.Add(InstallSourceMode.Local);
        }

        return (true, rewritten.ToArray());
    }

    /// <summary>
    /// Builds the argument string used to relaunch the CLI for a menu selection,
    /// appending <c>--source local</c> only for source-aware commands when in local mode.
    /// </summary>
    public static string SubcommandArgs(string commandName, bool localMode)
    {
        if (localMode && _sourceAwareCommands.Contains(commandName))
        {
            return $"{commandName} {SourceOption} {InstallSourceMode.Local}";
        }

        return commandName;
    }
}
