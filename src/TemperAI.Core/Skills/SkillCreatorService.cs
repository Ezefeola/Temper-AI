using System.IO;
using System.Text.Json;

namespace TemperAI.Core.Skills;

public sealed class SkillCreatorService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SkillResult CreateSkill(string outputDirectory, string category, string name, SkillMetadata metadata)
    {
        try
        {
            string skillDirectory = Path.Combine(outputDirectory, "skills", category, name);

            Directory.CreateDirectory(skillDirectory);

            List<string> createdFiles = [];

            string skillMdContent = GenerateSkillMd(metadata);
            string skillMdPath = Path.Combine(skillDirectory, "SKILL.md");
            File.WriteAllText(skillMdPath, skillMdContent);
            createdFiles.Add(skillMdPath);

            string metadataPath = Path.Combine(skillDirectory, "metadata.json");
            string metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
            File.WriteAllText(metadataPath, metadataJson);
            createdFiles.Add(metadataPath);

            string readmeContent = GenerateReadme(metadata);
            string readmePath = Path.Combine(skillDirectory, "README.md");
            File.WriteAllText(readmePath, readmeContent);
            createdFiles.Add(readmePath);

            return SkillResult.Success(skillDirectory, createdFiles);
        }
        catch (Exception exception)
        {
            return SkillResult.Failure(exception.Message);
        }
    }

    public SkillResult InstallSkill(string skillsDirectory, string sourcePath)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                return SkillResult.Failure($"Source directory not found: {sourcePath}");
            }

            string metadataPath = Path.Combine(sourcePath, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                return SkillResult.Failure("metadata.json not found in source directory.");
            }

            string metadataJson = File.ReadAllText(metadataPath);
            SkillMetadata? metadata = JsonSerializer.Deserialize<SkillMetadata>(metadataJson, _jsonOptions);

            if (metadata is null)
            {
                return SkillResult.Failure("Invalid metadata.json format.");
            }

            string destinationPath = Path.Combine(skillsDirectory, metadata.Category, metadata.Name);

            Directory.CreateDirectory(destinationPath);

            List<string> installedFiles = [];

            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                string destFile = Path.Combine(destinationPath, relativePath);

                string? destDirectory = Path.GetDirectoryName(destFile);

                if (!string.IsNullOrEmpty(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(sourceFile, destFile, overwrite: true);
                installedFiles.Add(relativePath);
            }

            return SkillResult.Success(destinationPath, installedFiles);
        }
        catch (Exception exception)
        {
            return SkillResult.Failure(exception.Message);
        }
    }

    public IReadOnlyList<SkillInfo> DiscoverSkills(string skillsDirectory)
    {
        List<SkillInfo> skills = [];

        if (!Directory.Exists(skillsDirectory))
        {
            return skills.AsReadOnly();
        }

        foreach (string categoryDir in Directory.GetDirectories(skillsDirectory))
        {
            string categoryName = Path.GetFileName(categoryDir);

            foreach (string skillDir in Directory.GetDirectories(categoryDir))
            {
                string skillName = Path.GetFileName(skillDir);
                string metadataPath = Path.Combine(skillDir, "metadata.json");

                SkillMetadata? metadata = null;

                if (File.Exists(metadataPath))
                {
                    string metadataJson = File.ReadAllText(metadataPath);
                    metadata = JsonSerializer.Deserialize<SkillMetadata>(metadataJson, _jsonOptions);
                }

                string[] files = Directory.GetFiles(skillDir, "*", SearchOption.AllDirectories);
                long totalSize = files.Sum(f => new FileInfo(f).Length);

                skills.Add(new SkillInfo
                {
                    Name = metadata?.Name ?? skillName,
                    Category = metadata?.Category ?? categoryName,
                    Version = metadata?.Version ?? "unknown",
                    Author = metadata?.Author ?? "unknown",
                    Description = metadata?.Description ?? "No description",
                    Tags = metadata?.Tags ?? [],
                    Path = skillDir,
                    FileCount = files.Length,
                    TotalSizeBytes = totalSize
                });
            }
        }

        return skills.AsReadOnly();
    }

    private static string GenerateSkillMd(SkillMetadata metadata)
    {
        return $"""
---
name: {metadata.Name}
description: >
  {metadata.Description}
  Category: {metadata.Category}
  Author: {metadata.Author}
  Version: {metadata.Version}
---

# {metadata.Name} — TemperAI Skill

> Created by {metadata.Author}
> Version: {metadata.Version}
> License: {metadata.License}

## When to use

[Describe when this skill should be loaded]

## When NOT to use

[Describe when this skill should NOT be loaded]

---

## Rules

[List the rules, patterns, and conventions that this skill enforces]

### Rule 1

[Description and code example]

### Rule 2

[Description and code example]

## Anti-patterns

[List what should NEVER be done when this skill is active]

## Templates

[Provide code templates that agents should use when this skill is active]

## Dependencies

This skill depends on:
{(metadata.Dependencies.Length > 0 ? string.Join("\n", metadata.Dependencies.Select(d => $"- `{d}`")) : "- None")}

""";
    }

    private static string GenerateReadme(SkillMetadata metadata)
    {
        return $"""
# {metadata.Name}

{metadata.Description}

## Metadata

| Field | Value |
|---|---|
| Name | {metadata.Name} |
| Version | {metadata.Version} |
| Author | {metadata.Author} |
| Category | {metadata.Category} |
| License | {metadata.License} |

## Tags

{(metadata.Tags.Length > 0 ? string.Join(", ", metadata.Tags) : "None")}

## Dependencies

{(metadata.Dependencies.Length > 0 ? string.Join("\n", metadata.Dependencies.Select(d => $"- `{d}`")) : "None")}

## How to use

1. Install this skill: `temper-ai skill-install --source <path-to-skill>`
2. Reference it in your agent's `.agent.md` file
3. The agent will load this skill when the conditions are met

## Contributing

[How others can contribute to this skill]

""";
    }
}
