using System.IO;

namespace TemperAI.Core.Snapshots;

public sealed class SnapshotService
{
    private static readonly string[] _trackedFiles =
    [
        "constitution.md",
        "spec.md",
        "design.md",
        "tasks.md",
        "budget.md"
    ];

    public SnapshotResult CreateSnapshot(string temperDirectory, string phaseName)
    {
        try
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string snapshotName = $"{timestamp}_{phaseName}";
            string snapshotPath = Path.Combine(temperDirectory, ".snapshots", snapshotName);

            Directory.CreateDirectory(snapshotPath);

            List<string> copiedFiles = [];

            foreach (string fileName in _trackedFiles)
            {
                string sourcePath = Path.Combine(temperDirectory, fileName);

                if (File.Exists(sourcePath))
                {
                    string destinationPath = Path.Combine(snapshotPath, fileName);
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    copiedFiles.Add(fileName);
                }
            }

            if (copiedFiles.Count == 0)
            {
                Directory.Delete(snapshotPath, recursive: true);
                return SnapshotResult.Failure("No tracked files found to snapshot.");
            }

            return SnapshotResult.Success(snapshotPath, copiedFiles);
        }
        catch (Exception exception)
        {
            return SnapshotResult.Failure(exception.Message);
        }
    }

    public SnapshotResult RestoreSnapshot(string temperDirectory, string snapshotName)
    {
        try
        {
            string snapshotPath = Path.Combine(temperDirectory, ".snapshots", snapshotName);

            if (!Directory.Exists(snapshotPath))
            {
                return SnapshotResult.Failure($"Snapshot not found: {snapshotName}");
            }

            List<string> restoredFiles = [];

            foreach (string fileName in _trackedFiles)
            {
                string sourcePath = Path.Combine(snapshotPath, fileName);
                string destinationPath = Path.Combine(temperDirectory, fileName);

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    restoredFiles.Add(fileName);
                }
            }

            if (restoredFiles.Count == 0)
            {
                return SnapshotResult.Failure("No files found in snapshot to restore.");
            }

            return SnapshotResult.Success(snapshotPath, restoredFiles);
        }
        catch (Exception exception)
        {
            return SnapshotResult.Failure(exception.Message);
        }
    }

    public IReadOnlyList<SnapshotInfo> ListSnapshots(string temperDirectory)
    {
        List<SnapshotInfo> snapshots = [];

        string snapshotsDirectory = Path.Combine(temperDirectory, ".snapshots");

        if (!Directory.Exists(snapshotsDirectory))
        {
            return snapshots.AsReadOnly();
        }

        foreach (string directory in Directory.GetDirectories(snapshotsDirectory))
        {
            string name = Path.GetFileName(directory);

            DateTime createdAt = DateTime.MinValue;

            if (name.Length >= 15 && DateTime.TryParseExact(
                name[..15].Replace("-", " ").Trim(),
                "yyyyMMdd HHmmss",
                null,
                System.Globalization.DateTimeStyles.None,
                out DateTime parsedDate))
            {
                createdAt = parsedDate;
            }

            string phase = name.Contains('_') ? name[(name.IndexOf('_') + 1)..] : "unknown";

            string[] files = Directory.GetFiles(directory);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            snapshots.Add(new SnapshotInfo
            {
                Name = name,
                Path = directory,
                CreatedAt = createdAt,
                Phase = phase,
                FileCount = files.Length,
                TotalSizeBytes = totalSize
            });
        }

        return snapshots.OrderByDescending(s => s.CreatedAt).ToList().AsReadOnly();
    }

    public bool DeleteSnapshot(string temperDirectory, string snapshotName)
    {
        try
        {
            string snapshotPath = Path.Combine(temperDirectory, ".snapshots", snapshotName);

            if (!Directory.Exists(snapshotPath))
            {
                return false;
            }

            Directory.Delete(snapshotPath, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public SnapshotInfo? GetLatestSnapshot(string temperDirectory)
    {
        IReadOnlyList<SnapshotInfo> snapshots = ListSnapshots(temperDirectory);
        return snapshots.Count > 0 ? snapshots[0] : null;
    }
}
