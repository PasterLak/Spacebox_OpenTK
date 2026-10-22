using Engine.Utils;
using Spacebox.Game;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Network;

public static class GameSetFolders
{
    private const string ModEntry = "mod";
    private const string DefaultEntry = "default";
    private const string ExtensionEntryPrefix = "extension/";

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
        return ComputeHash(modPath, Array.Empty<InstalledGameSet>());
    }

    public static string ComputeHash(GameSetStack stack)
    {
        return ComputeHash(stack.Base.Folder, stack.Extensions);
    }

    public static string ComputeHash(string modPath, IReadOnlyList<InstalledGameSet> extensions)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, ModEntry) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), DefaultEntry));
        foreach (var extension in extensions) folders.Add(new ArchiveFolder(extension.Folder, ExtensionEntryPrefix + extension.Id.ToLowerInvariant()));
        return DirectoryArchive.ComputeHash(folders);
    }

    public static byte[] Pack(string modPath)
    {
        return Pack(modPath, Array.Empty<InstalledGameSet>());
    }

    public static byte[] Pack(string modPath, IReadOnlyList<InstalledGameSet> extensions)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, FolderName(modPath)) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), Globals.GameSet.Default));
        foreach (var extension in extensions) folders.Add(new ArchiveFolder(extension.Folder, extension.FolderName));
        return DirectoryArchive.Pack(folders);
    }

    private static string FolderName(string path)
    {
        return Path.GetFileName(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar));
    }
}
