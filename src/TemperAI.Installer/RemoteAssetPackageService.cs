using System.IO.Compression;

namespace TemperAI.Installer;

public sealed class RemoteAssetPackageService
{
    public string DownloadAndExtract(string assetsUrl)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "temper-ai", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string zipPath = Path.Combine(tempDirectory, "assets.zip");

        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TemperAI/1.0");

        using HttpResponseMessage response = httpClient.GetAsync(assetsUrl).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using (FileStream output = File.Create(zipPath))
        {
            response.Content.CopyToAsync(output).GetAwaiter().GetResult();
        }

        string extractPath = Path.Combine(tempDirectory, "extracted");
        ZipFile.ExtractToDirectory(zipPath, extractPath);
        return extractPath;
    }
}
