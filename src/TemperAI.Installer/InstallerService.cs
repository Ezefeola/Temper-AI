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

            InstallDirectory(
                Path.Combine(assetsRoot, "skills"),
                target.SkillsPath,
                installed,
                skipped,
                errors,
                overwriteExisting);

            if (target.Format.Equals("claude", StringComparison.OrdinalIgnoreCase))
            {
                InstallClaudeAgents(
                    Path.Combine(assetsRoot, "agents"),
                    target,
                    installed,
                    skipped,
                    errors,
                    overwriteExisting);
            }
            else
            {
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
                ConvertedAgent converted = _claudeAssetConverter.Convert(sourceContent);

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
