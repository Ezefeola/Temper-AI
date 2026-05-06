using System.IO;

namespace TemperAI.Core.Incremental;

public sealed class IncrementalUpdateService
{
    private static readonly Dictionary<string, PhaseDependency> _phaseMap = new()
    {
        ["temper-analyst-prd"] = new PhaseDependency
        {
            PhaseName = "temper-analyst-prd",
            DependsOn = [],
            TrackedFiles = ["prd.md"],
            Status = "unknown"
        },
        ["temper-analyst-spec"] = new PhaseDependency
        {
            PhaseName = "temper-analyst-spec",
            DependsOn = ["temper-analyst-prd"],
            TrackedFiles = ["specs/INDEX.md"],
            Status = "unknown"
        },
        ["temper-architect"] = new PhaseDependency
        {
            PhaseName = "temper-architect",
            DependsOn = ["temper-analyst-spec"],
            TrackedFiles = ["backend-config.md", "frontend-config.md"],
            Status = "unknown"
        },
        ["temper-tasks"] = new PhaseDependency
        {
            PhaseName = "temper-tasks",
            DependsOn = ["temper-analyst-spec", "temper-architect"],
            TrackedFiles = ["tasks/INDEX.md"],
            Status = "unknown"
        },
        ["temper-plan"] = new PhaseDependency
        {
            PhaseName = "temper-plan",
            DependsOn = ["temper-architect", "temper-tasks"],
            TrackedFiles = ["build-plan.md"],
            Status = "unknown"
        },
        ["temper-review"] = new PhaseDependency
        {
            PhaseName = "temper-review",
            DependsOn = ["temper-architect", "temper-plan"],
            TrackedFiles = [],
            Status = "unknown"
        },
        ["temper-docs"] = new PhaseDependency
        {
            PhaseName = "temper-docs",
            DependsOn = ["temper-architect", "temper-plan", "temper-review"],
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
            "temper-analyst-prd",
            "temper-analyst-spec",
            "temper-architect",
            "temper-tasks",
            "temper-plan",
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
            ["temper-analyst-prd", "temper-analyst-spec", "temper-architect", "temper-tasks", "temper-plan", "temper-review", "temper-docs"],
            ["temper-analyst-spec", "temper-architect", "temper-tasks", "temper-plan", "temper-review", "temper-docs"],
            ["temper-architect", "temper-tasks", "temper-plan", "temper-review", "temper-docs"],
            ["temper-tasks", "temper-plan", "temper-review", "temper-docs"],
            ["temper-plan", "temper-review", "temper-docs"],
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
