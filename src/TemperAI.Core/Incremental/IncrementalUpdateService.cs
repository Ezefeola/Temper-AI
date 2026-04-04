using System.IO;

namespace TemperAI.Core.Incremental;

public sealed class IncrementalUpdateService
{
    private static readonly Dictionary<string, PhaseDependency> _phaseMap = new()
    {
        ["temper-init"] = new PhaseDependency
        {
            PhaseName = "temper-init",
            DependsOn = [],
            TrackedFiles = ["constitution.md"],
            Status = "unknown"
        },
        ["temper-spec"] = new PhaseDependency
        {
            PhaseName = "temper-spec",
            DependsOn = ["temper-init"],
            TrackedFiles = ["spec.md"],
            Status = "unknown"
        },
        ["temper-design"] = new PhaseDependency
        {
            PhaseName = "temper-design",
            DependsOn = ["temper-init", "temper-spec"],
            TrackedFiles = ["design.md"],
            Status = "unknown"
        },
        ["temper-tasks"] = new PhaseDependency
        {
            PhaseName = "temper-tasks",
            DependsOn = ["temper-init", "temper-spec", "temper-design"],
            TrackedFiles = ["tasks.md"],
            Status = "unknown"
        },
        ["temper-build"] = new PhaseDependency
        {
            PhaseName = "temper-build",
            DependsOn = ["temper-init", "temper-spec", "temper-design", "temper-tasks"],
            TrackedFiles = [],
            Status = "unknown"
        },
        ["temper-review"] = new PhaseDependency
        {
            PhaseName = "temper-review",
            DependsOn = ["temper-init", "temper-spec", "temper-design", "temper-build"],
            TrackedFiles = [],
            Status = "unknown"
        },
        ["temper-docs"] = new PhaseDependency
        {
            PhaseName = "temper-docs",
            DependsOn = ["temper-init", "temper-spec", "temper-design", "temper-tasks", "temper-build", "temper-review"],
            TrackedFiles = [],
            Status = "unknown"
        }
    };

    public IncrementalResult AnalyzeChanges(string temperDirectory)
    {
        if (!Directory.Exists(temperDirectory))
        {
            return IncrementalResult.NoChanges();
        }

        string snapshotsDirectory = Path.Combine(temperDirectory, ".snapshots");

        if (!Directory.Exists(snapshotsDirectory))
        {
            return IncrementalResult.NoChanges();
        }

        string? latestSnapshot = GetLatestSnapshotDirectory(snapshotsDirectory);

        if (latestSnapshot is null)
        {
            return IncrementalResult.NoChanges();
        }

        List<string> changedFiles = [];
        List<string> affectedPhases = [];
        List<string> unchangedPhases = [];

        foreach (var entry in _phaseMap)
        {
            string phaseName = entry.Key;
            PhaseDependency phase = entry.Value;

            bool hasChanges = false;

            foreach (string trackedFile in phase.TrackedFiles)
            {
                string currentPath = Path.Combine(temperDirectory, trackedFile);
                string snapshotPath = Path.Combine(latestSnapshot, trackedFile);

                bool fileChanged = HasFileChanged(currentPath, snapshotPath);

                if (fileChanged)
                {
                    hasChanges = true;
                    if (!changedFiles.Contains(trackedFile))
                    {
                        changedFiles.Add(trackedFile);
                    }
                }
            }

            if (hasChanges)
            {
                affectedPhases.Add(phaseName);
            }
            else
            {
                unchangedPhases.Add(phaseName);
            }
        }

        List<string> cascadedPhases = GetCascadedPhases(affectedPhases);

        foreach (string phase in cascadedPhases)
        {
            if (!affectedPhases.Contains(phase))
            {
                affectedPhases.Add(phase);
                unchangedPhases.Remove(phase);
            }
        }

        if (affectedPhases.Count == 0)
        {
            return IncrementalResult.NoChanges();
        }

        return IncrementalResult.ChangesDetected(changedFiles, affectedPhases, unchangedPhases);
    }

    public IReadOnlyList<PhaseDependency> GetReRunOrder(List<string> affectedPhases)
    {
        List<PhaseDependency> orderedPhases = [];

        string[] executionOrder =
        [
            "temper-init",
            "temper-spec",
            "temper-design",
            "temper-tasks",
            "temper-build",
            "temper-review",
            "temper-docs"
        ];

        foreach (string phaseName in executionOrder)
        {
            if (affectedPhases.Contains(phaseName) && _phaseMap.TryGetValue(phaseName, out PhaseDependency? phase))
            {
                orderedPhases.Add(phase);
            }
        }

        return orderedPhases.AsReadOnly();
    }

    private static bool HasFileChanged(string currentPath, string snapshotPath)
    {
        bool currentExists = File.Exists(currentPath);
        bool snapshotExists = File.Exists(snapshotPath);

        if (currentExists && !snapshotExists)
        {
            return true;
        }

        if (!currentExists && snapshotExists)
        {
            return true;
        }

        if (!currentExists)
        {
            return false;
        }

        string currentContent = File.ReadAllText(currentPath);
        string snapshotContent = File.ReadAllText(snapshotPath);

        return currentContent != snapshotContent;
    }

    private static string? GetLatestSnapshotDirectory(string snapshotsDirectory)
    {
        string[] directories = Directory.GetDirectories(snapshotsDirectory);

        if (directories.Length == 0)
        {
            return null;
        }

        return directories.OrderByDescending(d => d).First();
    }

    private static List<string> GetCascadedPhases(List<string> directlyAffectedPhases)
    {
        List<string> cascaded = new(directlyAffectedPhases);

        string[][] cascadeRules =
        [
            ["temper-init", "temper-spec", "temper-design", "temper-tasks", "temper-build", "temper-review", "temper-docs"],
            ["temper-spec", "temper-design", "temper-tasks", "temper-build", "temper-review", "temper-docs"],
            ["temper-design", "temper-tasks", "temper-build", "temper-review", "temper-docs"],
            ["temper-tasks", "temper-build", "temper-review", "temper-docs"],
            ["temper-build", "temper-review", "temper-docs"],
            ["temper-review", "temper-docs"]
        ];

        foreach (string[] cascade in cascadeRules)
        {
            if (cascaded.Contains(cascade[0]))
            {
                for (int i = 1; i < cascade.Length; i++)
                {
                    if (!cascaded.Contains(cascade[i]))
                    {
                        cascaded.Add(cascade[i]);
                    }
                }
            }
        }

        return cascaded;
    }
}
