using System.IO.Compression;

namespace TemperAI.Installer;

public sealed class CliSelfUpdateService
{
    public string DownloadAndStageCli(string cliUrl)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "temper-ai", "cli-update", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string downloadPath = Path.Combine(tempDirectory, Path.GetFileName(new Uri(cliUrl).LocalPath));

        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TemperAI/1.0");

        using HttpResponseMessage response = httpClient.GetAsync(cliUrl).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using (FileStream output = File.Create(downloadPath))
        {
            response.Content.CopyToAsync(output).GetAwaiter().GetResult();
        }

        if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            string extractPath = Path.Combine(tempDirectory, "extract");
            ZipFile.ExtractToDirectory(downloadPath, extractPath);

            string? exePath = Directory.GetFiles(extractPath, "temper-ai.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (exePath is null)
            {
                throw new InvalidOperationException("El paquete CLI descargado no contiene temper-ai.exe.");
            }

            return exePath;
        }

        return downloadPath;
    }

    public string StageReplacement(string downloadedExePath)
    {
        Directory.CreateDirectory(InstallationPaths.InstallRoot);

        string finalExe = InstallationPaths.CliExePath;
        string stagedExe = Path.Combine(InstallationPaths.InstallRoot, "temper-ai.next.exe");
        File.Copy(downloadedExePath, stagedExe, overwrite: true);

        string scriptPath = Path.Combine(InstallationPaths.InstallRoot, "apply-update.cmd");
        string scriptContent = $"@echo off{Environment.NewLine}" +
                               "ping 127.0.0.1 -n 3 > nul" + Environment.NewLine +
                               $"copy /Y \"{stagedExe}\" \"{finalExe}\" > nul" + Environment.NewLine +
                               $"del \"{stagedExe}\" > nul 2>&1" + Environment.NewLine +
                               $"del \"%~f0\" > nul 2>&1" + Environment.NewLine;

        File.WriteAllText(scriptPath, scriptContent);
        return scriptPath;
    }
}
