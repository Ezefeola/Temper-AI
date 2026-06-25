using TemperAI.Core.Models;

namespace TemperAI.Installer;
public sealed class InstallerService
{
    private readonly bool _dryRun;
    private readonly LocalAssetSourceService _localAssetSourceService = new();
    private readonly RemoteAssetPackageService _remoteAssetPackageService = new();
    private readonly ReleaseManifestService _releaseManifestService = new();
    private readonly InstallMetadataService _installMetadataService = new();
    private readonly ClaudeAssetConverter _claudeAssetConverter = new();
    private readonly ClaudeSkillConverter _claudeSkillConverter = new();

    private const string SkillFileName = "SKILL.md";

    private static readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md"
    };

    public InstallerService(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public InstallResult Install(AgentTarget target, string sourceMode = InstallSourceMode.Remote, bool overwriteExisting = false)
    {
        List<string> installed = [];
        List<string> skipped = [];
        List<string> errors = [];

        string normalizedSourceMode = InstallSourceMode.Normalize(sourceMode);
        string installedAssetsVersion = normalizedSourceMode.Equals(InstallSourceMode.Local, StringComparison.OrdinalIgnoreCase)
            ? "local"
            : string.Empty;

        try
        {
            string assetsRoot = normalizedSourceMode.Equals(InstallSourceMode.Local, StringComparison.OrdinalIgnoreCase)
                ? _localAssetSourceService.ResolveAssetsRoot()
                : ResolveRemoteAssetsRoot(out installedAssetsVersion);

            assetsRoot = NormalizeAssetsRoot(assetsRoot);

            if (target.Format.Equals("claude", StringComparison.OrdinalIgnoreCase))
            {
                string skillsSourceDirectory = Path.Combine(assetsRoot, "skills");

                // Build the flat-name map once from the skills source so that skills and
                // agents are rewritten with identical flat names.
                SkillFlatNameMap? skillFlatNameMap = TryBuildSkillFlatNameMap(skillsSourceDirectory, errors);

                if (skillFlatNameMap is not null)
                {
                    InstallClaudeSkills(
                        skillsSourceDirectory,
                        skillFlatNameMap,
                        target,
                        installed,
                        skipped,
                        errors,
                        overwriteExisting);

                    InstallClaudeAgents(
                        Path.Combine(assetsRoot, "agents"),
                        skillFlatNameMap,
                        target,
                        installed,
                        skipped,
                        errors,
                        overwriteExisting);
                }
            }
            else
            {
                InstallDirectory(
                    Path.Combine(assetsRoot, "skills"),
                    target.SkillsPath,
                    installed,
                    skipped,
                    errors,
                    overwriteExisting);

                InstallDirectory(
                    Path.Combine(assetsRoot, "agents"),
                    target.AgentsPath,
                    installed,
                    skipped,
                    errors,
                    overwriteExisting);
            }

            if (!_dryRun && errors.Count == 0)
            {
                string cliVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                    ?? typeof(InstallerService).Assembly.GetName().Version?.ToString()
                    ?? string.Empty;
                DateTimeOffset now = DateTimeOffset.UtcNow;

                _installMetadataService.Save(new InstallMetadata
                {
                    Channel = "stable",
                    SourceMode = normalizedSourceMode,
                    ManifestUrl = normalizedSourceMode.Equals(InstallSourceMode.Remote, StringComparison.OrdinalIgnoreCase)
                        ? ReleaseManifestService.StableManifestUrl
                        : string.Empty,
                    InstalledCliVersion = cliVersion,
                    InstalledAssetsVersion = installedAssetsVersion,
                    InstalledAt = now,
                    LastUpdatedAt = now
                });
            }
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
        }

        return new InstallResult
        {
            Target = target,
            Installed = installed,
            Skipped = skipped,
            Errors = errors,
            IsSuccess = errors.Count == 0
        };
    }

    private void InstallDirectory(
        string sourceDirectory,
        string destinationDirectory,
        List<string> installed,
        List<string> skipped,
        List<string> errors,
        bool overwriteExisting)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            errors.Add($"Directorio de origen no encontrado: {sourceDirectory}");
            return;
        }

        foreach (string sourcePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(sourcePath);

            if (_ignoredFiles.Contains(fileName))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(sourceDirectory, sourcePath);
            string destinationPath = Path.Combine(destinationDirectory, relativePath);

            if (_dryRun)
            {
                installed.Add($"{destinationPath} (dry run)");
                continue;
            }

            if (File.Exists(destinationPath) && !overwriteExisting)
            {
                skipped.Add(destinationPath);
                continue;
            }

            try
            {
                string? directory = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourcePath, destinationPath, overwrite: overwriteExisting);
                installed.Add(destinationPath);
            }
            catch (Exception exception)
            {
                errors.Add($"{destinationPath}: {exception.Message}");
            }
        }
    }

    private void InstallClaudeAgents(
        string sourceDirectory,
        SkillFlatNameMap skillFlatNameMap,
        AgentTarget target,
        List<string> installed,
        List<string> skipped,
        List<string> errors,
        bool overwriteExisting)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            errors.Add($"Directorio de origen no encontrado: {sourceDirectory}");
            return;
        }

        foreach (string sourcePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(sourcePath);

            if (_ignoredFiles.Contains(fileName))
            {
                continue;
            }

            try
            {
                string sourceContent = File.ReadAllText(sourcePath);
                ConvertedAgent converted = _claudeAssetConverter.Convert(sourceContent, skillFlatNameMap);

                string destinationPath = Path.Combine(target.AgentsPath, converted.FileName);

                if (_dryRun)
                {
                    installed.Add($"{destinationPath} (dry run)");
                    continue;
                }

                if (File.Exists(destinationPath) && !overwriteExisting)
                {
                    skipped.Add(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(target.AgentsPath);
                File.WriteAllText(destinationPath, converted.Content);
                installed.Add(destinationPath);
            }
            catch (Exception exception)
            {
                errors.Add($"{sourcePath}: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Installs skills for the Claude target, flattening the deep nested asset layout into the
    /// one-level <c>&lt;SkillsPath&gt;/&lt;flat-name&gt;/SKILL.md</c> layout Claude Code requires for
    /// discovery. Non-SKILL.md companion files (if any) are flattened into the same skill folder.
    /// Mirrors <see cref="InstallClaudeAgents"/> for dry-run, overwrite, ignored-file, and
    /// installed/skipped/errors accounting.
    /// </summary>
    private void InstallClaudeSkills(
        string sourceDirectory,
        SkillFlatNameMap flatNameMap,
        AgentTarget target,
        List<string> installed,
        List<string> skipped,
        List<string> errors,
        bool overwriteExisting)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            errors.Add($"Directorio de origen no encontrado: {sourceDirectory}");
            return;
        }

        string[] sourcePaths = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

        foreach (string sourcePath in sourcePaths)
        {
            string fileName = Path.GetFileName(sourcePath);

            if (_ignoredFiles.Contains(fileName))
            {
                continue;
            }

            try
            {
                string relativeDirectory = Path.GetDirectoryName(
                    Path.GetRelativePath(sourceDirectory, sourcePath)) ?? string.Empty;
                string flatName = flatNameMap.ResolveFlatName(relativeDirectory);
                string destinationPath = Path.Combine(target.SkillsPath, flatName, fileName);

                if (_dryRun)
                {
                    installed.Add($"{destinationPath} (dry run)");
                    continue;
                }

                if (File.Exists(destinationPath) && !overwriteExisting)
                {
                    skipped.Add(destinationPath);
                    continue;
                }

                Directory.CreateDirectory(Path.Combine(target.SkillsPath, flatName));

                if (fileName.Equals(SkillFileName, StringComparison.OrdinalIgnoreCase))
                {
                    string sourceContent = File.ReadAllText(sourcePath);
                    ConvertedSkill converted = _claudeSkillConverter.Convert(sourceContent, relativeDirectory, flatNameMap);
                    File.WriteAllText(destinationPath, converted.Content);
                }
                else
                {
                    File.Copy(sourcePath, destinationPath, overwrite: overwriteExisting);
                }

                installed.Add(destinationPath);
            }
            catch (Exception exception)
            {
                errors.Add($"{sourcePath}: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Builds the shared <see cref="SkillFlatNameMap"/> from the skills source directory so the
    /// same flat names drive both skill installation and agent reference rewriting. Returns
    /// <see langword="null"/> (recording an error) when the source is missing or the map cannot
    /// be built, so the Claude install reports the failure instead of proceeding inconsistently.
    /// </summary>
    private static SkillFlatNameMap? TryBuildSkillFlatNameMap(string skillsSourceDirectory, List<string> errors)
    {
        if (!Directory.Exists(skillsSourceDirectory))
        {
            errors.Add($"Directorio de origen no encontrado: {skillsSourceDirectory}");
            return null;
        }

        string[] sourcePaths = Directory.GetFiles(skillsSourceDirectory, "*", SearchOption.AllDirectories);

        try
        {
            return SkillFlatNameMap.Build(
                EnumerateSkillRelativeDirectories(skillsSourceDirectory, sourcePaths));
        }
        catch (Exception exception)
        {
            errors.Add(exception.Message);
            return null;
        }
    }

    private static IEnumerable<string> EnumerateSkillRelativeDirectories(string sourceDirectory, IEnumerable<string> sourcePaths)
    {
        foreach (string sourcePath in sourcePaths)
        {
            if (!Path.GetFileName(sourcePath).Equals(SkillFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return Path.GetDirectoryName(
                Path.GetRelativePath(sourceDirectory, sourcePath)) ?? string.Empty;
        }
    }

    private string ResolveRemoteAssetsRoot(out string assetsVersion)
    {
        ReleaseManifest manifest = _releaseManifestService.DownloadStableManifest();
        assetsVersion = manifest.Assets.Version;
        return _remoteAssetPackageService.DownloadAndExtract(manifest.Assets.Url);
    }

    private static string NormalizeAssetsRoot(string assetsRoot)
    {
        if (Directory.Exists(Path.Combine(assetsRoot, "skills")) ||
            Directory.Exists(Path.Combine(assetsRoot, "agents")))
        {
            return assetsRoot;
        }

        string nestedAssetsRoot = Path.Combine(assetsRoot, "assets");

        if (Directory.Exists(Path.Combine(nestedAssetsRoot, "skills")) ||
            Directory.Exists(Path.Combine(nestedAssetsRoot, "agents")))
        {
            return nestedAssetsRoot;
        }

        return assetsRoot;
    }
}
