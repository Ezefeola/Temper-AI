namespace TemperAI.Installer;

public static class InstallationPaths
{
    public static string InstallRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "TemperAI");

    public static string CliExePath => Path.Combine(InstallRoot, "temper-ai.exe");

    public static string StateDirectory => Path.Combine(InstallRoot, "state");

    public static string MetadataPath => Path.Combine(StateDirectory, "install-metadata.json");
}
