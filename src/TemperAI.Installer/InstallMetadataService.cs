using System.Text.Json;
using TemperAI.Core.Models;

namespace TemperAI.Installer;

public sealed class InstallMetadataService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public InstallMetadata? Load()
    {
        if (!File.Exists(InstallationPaths.MetadataPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(InstallationPaths.MetadataPath);
            return JsonSerializer.Deserialize<InstallMetadata>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(InstallMetadata metadata)
    {
        Directory.CreateDirectory(InstallationPaths.StateDirectory);
        string json = JsonSerializer.Serialize(metadata, _jsonOptions);
        File.WriteAllText(InstallationPaths.MetadataPath, json);
    }
}
