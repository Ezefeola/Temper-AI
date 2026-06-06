using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;
using TemperAI.Core.Configuration;
using TemperAI.Core.Models;
using TemperAI.Installer;

namespace TemperAI.Cli.Commands;

public sealed class UpdateSettings : CommandSettings
{
    [CommandOption("--force")]
    [Description("Actualiza sin pedir confirmación")]
    public bool Force { get; init; }

    [CommandOption("--dry-run")]
    [Description("Muestra qué se actualizaría sin escribir archivos")]
    public bool DryRun { get; init; }

    [CommandOption("-a|--agent")]
    [Description("ID del agente a actualizar (opencode)")]
    public string? AgentId { get; init; }

    [CommandOption("--source")]
    [Description("Origen de assets: remote (default) o local")]
    public string? SourceMode { get; init; }
}

public sealed class UpdateCommand : Command<UpdateSettings>
{
    public override int Execute(CommandContext context, UpdateSettings settings)
    {
        PrintHeader();

        if (!string.IsNullOrWhiteSpace(settings.SourceMode) && !InstallSourceMode.IsValid(settings.SourceMode))
        {
            AnsiConsole.MarkupLine("[red]Source invalido. Usá 'remote' o 'local'.[/]");
            return 1;
        }

        string sourceMode = InstallSourceMode.Normalize(settings.SourceMode);
        IReadOnlyList<AgentTarget> supportedTargets = AgentTargets.Supported();
        List<AgentTarget> selectedTargets = ResolveTargets(settings, supportedTargets);

        if (selectedTargets.Count == 0)
        {
            return 1;
        }

        InstallMetadataService metadataService = new();
        InstallMetadata? metadata = metadataService.Load();
        ReleaseManifest? manifest = null;
        string localCliVersion = GetCliVersion();
        bool cliNeedsUpdate = false;
        bool assetsNeedUpdate = metadata is null || !string.Equals(metadata.SourceMode, sourceMode, StringComparison.OrdinalIgnoreCase);

        if (sourceMode.Equals(InstallSourceMode.Remote, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                ReleaseManifestService manifestService = new();
                manifest = manifestService.DownloadStableManifest(metadata?.ManifestUrl);
                cliNeedsUpdate = IsRemoteVersionDifferent(localCliVersion, manifest.Cli.Version);
                assetsNeedUpdate = assetsNeedUpdate || metadata?.InstalledAssetsVersion != manifest.Assets.Version;
            }
            catch (Exception exception)
            {
                AnsiConsole.MarkupLine($"[red]No se pudo consultar el manifest remoto:[/] {exception.Message}");
                return 1;
            }
        }
        else
        {
            assetsNeedUpdate = true;
        }

        if (!cliNeedsUpdate && !assetsNeedUpdate)
        {
            AnsiConsole.MarkupLine("[green]Todo está actualizado.[/]");
            return 0;
        }

        ShowPlan(sourceMode, cliNeedsUpdate, assetsNeedUpdate, manifest);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry-run completado.[/]");
            return 0;
        }

        if (!settings.Force && !AnsiConsole.Confirm("¿Querés aplicar esta actualización?"))
        {
            AnsiConsole.MarkupLine("[dim]Actualización cancelada.[/]");
            return 0;
        }

        bool success = true;

        if (assetsNeedUpdate)
        {
            InstallerService installerService = new();

            foreach (AgentTarget target in selectedTargets)
            {
                InstallResult result = installerService.Install(target, sourceMode, overwriteExisting: true);
                PrintInstallResult(result);
                success &= result.IsSuccess;
            }
        }

        if (success && cliNeedsUpdate && manifest is not null)
        {
            try
            {
                ReleaseManifestService manifestService = new();
                CliPlatformManifest cliArtifact = manifestService.GetCurrentPlatformArtifact(manifest);
                CliSelfUpdateService cliSelfUpdateService = new();
                string downloadedExe = cliSelfUpdateService.DownloadAndStageCli(cliArtifact.Url);
                string scriptPath = cliSelfUpdateService.StageReplacement(downloadedExe);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{scriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                AnsiConsole.MarkupLine("[green]✓[/] Se preparó la actualización del CLI.");
                AnsiConsole.MarkupLine("[yellow]Reiniciá TemperAI o abrí una nueva terminal para usar la nueva versión.[/]");

                metadataService.Save(new InstallMetadata
                {
                    Channel = manifest.Channel,
                    SourceMode = sourceMode,
                    ManifestUrl = metadata?.ManifestUrl ?? ReleaseManifestService.StableManifestUrl,
                    InstalledCliVersion = manifest.Cli.Version,
                    InstalledAssetsVersion = manifest.Assets.Version,
                    InstalledAt = metadata?.InstalledAt ?? DateTimeOffset.UtcNow,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception exception)
            {
                success = false;
                AnsiConsole.MarkupLine($"[red]No se pudo actualizar el CLI:[/] {exception.Message}");
            }
        }
        else if (success && manifest is not null && assetsNeedUpdate)
        {
            metadataService.Save(new InstallMetadata
            {
                Channel = manifest.Channel,
                SourceMode = sourceMode,
                ManifestUrl = metadata?.ManifestUrl ?? ReleaseManifestService.StableManifestUrl,
                InstalledCliVersion = localCliVersion,
                InstalledAssetsVersion = manifest.Assets.Version,
                InstalledAt = metadata?.InstalledAt ?? DateTimeOffset.UtcNow,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
        }

        return success ? 0 : 1;
    }

    private static void ShowPlan(string sourceMode, bool cliNeedsUpdate, bool assetsNeedUpdate, ReleaseManifest? manifest)
    {
        Table table = new();
        table.AddColumn("[bold]Componente[/]");
        table.AddColumn("[bold]Acción[/]");

        table.AddRow("Source mode", sourceMode);

        if (assetsNeedUpdate)
        {
            table.AddRow("Assets", sourceMode == InstallSourceMode.Remote
                ? $"Actualizar a {manifest?.Assets.Version ?? "latest"}"
                : "Sincronizar desde repo local");
        }

        if (cliNeedsUpdate)
        {
            table.AddRow("CLI", $"Actualizar a {manifest?.Cli.Version ?? "latest"}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void PrintInstallResult(InstallResult installResult)
    {
        if (installResult.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {installResult.Summary()}");
            return;
        }

        AnsiConsole.MarkupLine($"[red]✗[/] {installResult.Summary()}");

        foreach (string error in installResult.Errors)
        {
            AnsiConsole.MarkupLine($"  [red]•[/] {error}");
        }
    }

    private static bool IsRemoteVersionDifferent(string localVersion, string remoteVersion)
    {
        return NormalizeVersion(localVersion) != NormalizeVersion(remoteVersion);
    }

    private static string NormalizeVersion(string version)
    {
        string normalized = version.Trim().TrimStart('v', 'V');

        if (!Version.TryParse(normalized, out Version? parsedVersion))
        {
            return normalized;
        }

        return parsedVersion.Build >= 0
            ? $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}"
            : $"{parsedVersion.Major}.{parsedVersion.Minor}";
    }

    private static string GetCliVersion()
    {
        Version? version = typeof(UpdateCommand).Assembly.GetName().Version;
        return version?.ToString() ?? "0.0.0";
    }

    private static List<AgentTarget> ResolveTargets(UpdateSettings settings, IReadOnlyList<AgentTarget> supportedTargets)
    {
        if (!string.IsNullOrEmpty(settings.AgentId))
        {
            AgentTarget? agentTarget = AgentTargets.FindById(settings.AgentId);

            if (agentTarget is null)
            {
                AnsiConsole.MarkupLine($"[red]Agente no encontrado: {settings.AgentId}[/]");
                return [];
            }

            return [agentTarget];
        }

        return supportedTargets.ToList();
    }

    private static void PrintHeader()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new FigletText("TemperAI")
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Actualización unificada de CLI y assets[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]─────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }
}
