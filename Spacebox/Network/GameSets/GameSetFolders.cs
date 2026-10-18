using Engine.Utils;
using Spacebox.Game;

namespace Spacebox.Network;

public static class GameSetFolders
{
    private const string ModEntry = "mod";
    private const string DefaultEntry = "default";

    public static string DefaultPathNextTo(string modPath)
    {
        return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(modPath)), Globals.GameSet.Default);
    }

    public static bool IsDefault(string modPath)
    {
        return string.Equals(Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(DefaultPathNextTo(modPath)).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    public static string ComputeHash(string modPath)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, ModEntry) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), DefaultEntry));
        return DirectoryArchive.ComputeHash(folders);
    }

    public static byte[] Pack(string modPath)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, Path.GetFileName(Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar))) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), Globals.GameSet.Default));
        return DirectoryArchive.Pack(folders);
    }
}
