using System.Text.Json;
using TemperAI.Core.Models;

namespace TemperAI.Installer;

public sealed class ReleaseManifestService
{
    public const string StableManifestUrl = "https://github.com/Ezefeola/temper-ai/releases/latest/download/manifest.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReleaseManifest DownloadStableManifest(string? manifestUrl = null)
    {
        using HttpClient httpClient = CreateClient();
        string resolvedManifestUrl = string.IsNullOrWhiteSpace(manifestUrl)
            ? StableManifestUrl
            : manifestUrl;

        string json = httpClient.GetStringAsync(resolvedManifestUrl).GetAwaiter().GetResult();

        ReleaseManifest? manifest = JsonSerializer.Deserialize<ReleaseManifest>(json, _jsonOptions);

        if (manifest is null)
        {
            throw new InvalidOperationException("No se pudo deserializar manifest.json.");
        }

        return manifest;
    }

    public CliPlatformManifest GetCurrentPlatformArtifact(ReleaseManifest manifest)
    {
        const string rid = "win-x64";

        CliPlatformManifest? artifact = manifest.Cli.Platforms
            .FirstOrDefault(platform => platform.Rid.Equals(rid, StringComparison.OrdinalIgnoreCase));

        if (artifact is null)
        {
            throw new InvalidOperationException($"El manifest no define un artefacto CLI para {rid}.");
        }

        return artifact;
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TemperAI/1.0");
        return client;
    }
}
