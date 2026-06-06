namespace TemperAI.Installer;

public static class InstallSourceMode
{
    public const string Remote = "remote";
    public const string Local = "local";

    public static bool IsValid(string? sourceMode)
    {
        return sourceMode is not null &&
               (sourceMode.Equals(Remote, StringComparison.OrdinalIgnoreCase)
                || sourceMode.Equals(Local, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string? sourceMode)
    {
        return sourceMode?.Equals(Local, StringComparison.OrdinalIgnoreCase) == true
            ? Local
            : Remote;
    }
}
