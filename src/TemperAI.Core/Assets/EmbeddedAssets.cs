using System.Reflection;

namespace TemperAI.Core.Assets;

// Los archivos en /assets se compilan dentro del .exe en build time
public static class EmbeddedAssets
{
    private static readonly Assembly _assembly = typeof(EmbeddedAssets).Assembly;

    // Devuelve el contenido de un asset embebido como string
    // path debe ser relativo a la carpeta assets/ — ej: "skills/dotnet-api/SKILL.md"
    public static (bool Found, string Content) TryReadText(string path)
    {
        string resourceName = $"assets/{path}".Replace('\\', '/');

        using Stream? stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return (false, string.Empty);
        }

        using StreamReader reader = new(stream);
        return (true, reader.ReadToEnd());
    }

    // Lista todos los assets embebidos bajo un prefijo dado
    public static IReadOnlyList<string> ListPaths(string prefix = "")
    {
        string normalizedPrefix = string.IsNullOrEmpty(prefix)
            ? "assets/"
            : $"assets/{prefix.TrimEnd('/')}/";

        normalizedPrefix = normalizedPrefix.Replace('\\', '/');

        return _assembly
            .GetManifestResourceNames()
            .Select(name => name.Replace('\\', '/'))
            .Where(name => name.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[("assets/".Length)..])
            .ToList()
            .AsReadOnly();
    }

    // Copia un asset embebido a una ruta en disco
    public static (bool Success, string Error) CopyToDisk(string assetPath, string destinationPath)
    {
        string resourceName = $"assets/{assetPath}".Replace('\\', '/');

        using Stream? stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return (false, $"Asset no encontrado: {assetPath}");
        }

        try
        {
            string? directory = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using FileStream fileStream = File.Create(destinationPath);
            stream.CopyTo(fileStream);

            return (true, string.Empty);
        }
        catch (Exception exception)
        {
            return (false, exception.Message);
        }
    }
}