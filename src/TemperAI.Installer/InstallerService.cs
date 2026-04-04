using TemperAI.Core.Assets;
using TemperAI.Core.Models;

namespace TemperAI.Installer;
public sealed class InstallerService
{
    private readonly bool _dryRun;

    // Archivos que existen solo para que el embed funcione — no se instalan
    private static readonly HashSet<string> _ignoredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md"
    };

    public InstallerService(bool dryRun = false)
    {
        _dryRun = dryRun;
    }

    public InstallResult Install(AgentTarget target)
    {
        List<string> installed = [];
        List<string> skipped = [];
        List<string> errors = [];

        InstallDirectory("skills", target.SkillsPath, installed, skipped, errors);
        InstallDirectory("agents", target.AgentsPath, installed, skipped, errors);

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
        string assetPrefix,
        string destinationDirectory,
        List<string> installed,
        List<string> skipped,
        List<string> errors)
    {
        IReadOnlyList<string> assetPaths = EmbeddedAssets.ListPaths(assetPrefix);

        foreach (string assetPath in assetPaths)
        {
            string fileName = Path.GetFileName(assetPath);

            if (_ignoredFiles.Contains(fileName))
            {
                continue;
            }

            // Construimos el path relativo desde el prefijo
            // assets/skills/dotnet-api/SKILL.md → dotnet-api/SKILL.md
            string relativePath = assetPath[(assetPrefix.Length + 1)..];
            string destinationPath = Path.Combine(destinationDirectory, relativePath);

            if (_dryRun)
            {
                installed.Add($"{destinationPath} (dry run)");
                continue;
            }

            // Si el archivo ya existe lo saltamos — no sobreescribimos configs del usuario
            if (File.Exists(destinationPath))
            {
                skipped.Add(destinationPath);
                continue;
            }

            var (success, error) = EmbeddedAssets.CopyToDisk(assetPath, destinationPath);

            if (success)
            {
                installed.Add(destinationPath);
            }
            else
            {
                errors.Add($"{destinationPath}: {error}");
            }
        }
    }
}