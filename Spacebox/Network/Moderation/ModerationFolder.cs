namespace Spacebox.Network.Moderation;

public static class ModerationFolder
{
    public static string? Resolve(string? loadedWorldFolder, string? configuredWorldFolder)
    {
        if (!string.IsNullOrEmpty(loadedWorldFolder)) return loadedWorldFolder;
        return string.IsNullOrEmpty(configuredWorldFolder) || !Directory.Exists(configuredWorldFolder) ? null : configuredWorldFolder;
    }

    public static string? DedicatedWorld(string worldsDirectory, string? worldName)
    {
        if (string.IsNullOrWhiteSpace(worldsDirectory) || string.IsNullOrWhiteSpace(worldName)) return null;
        return Path.Combine(worldsDirectory, worldName);
    }
}
