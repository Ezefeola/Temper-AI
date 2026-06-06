namespace TemperAI.Installer;

public sealed class LocalAssetSourceService
{
    public string ResolveAssetsRoot()
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(currentDirectory, "assets");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            DirectoryInfo? parent = Directory.GetParent(currentDirectory);

            if (parent is null)
            {
                break;
            }

            currentDirectory = parent.FullName;
        }

        throw new InvalidOperationException("No se encontro la carpeta assets para source mode local.");
    }
}
