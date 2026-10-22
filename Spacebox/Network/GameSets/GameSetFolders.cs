using Engine.Utils;
using Spacebox.Game;
using Spacebox.Game.Resource.Content;
using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Network;

public static class GameSetFolders
{
    private const string ModEntry = "mod";
    private const string DefaultEntry = "default";
    private const string ExtensionEntryPrefix = "extension/";
    private const string TexturePackEntryPrefix = "texturepack/";

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

    public static string ComputeHash(GameSetStack stack, IReadOnlyList<TexturePack> texturePacks = null)
    {
        return ComputeHash(stack.Base.Folder, stack.Extensions, texturePacks);
    }

    public static string ComputeHash(string modPath, IReadOnlyList<InstalledGameSet> extensions, IReadOnlyList<TexturePack> texturePacks = null)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, ModEntry) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), DefaultEntry));
        foreach (var extension in extensions) folders.Add(new ArchiveFolder(extension.Folder, ExtensionEntryPrefix + extension.Id.ToLowerInvariant()));
        if (texturePacks != null)
            foreach (var pack in texturePacks) folders.Add(new ArchiveFolder(pack.Folder, TexturePackEntryPrefix + pack.Id.ToLowerInvariant()));
        return DirectoryArchive.ComputeHash(folders);
    }

    public static byte[] Pack(string modPath, IReadOnlyList<InstalledGameSet> extensions, IReadOnlyList<TexturePack> texturePacks = null)
    {
        var folders = new List<ArchiveFolder> { new ArchiveFolder(modPath, Entry(ModFolders.GameSetsName, FolderName(modPath))) };
        if (!IsDefault(modPath)) folders.Add(new ArchiveFolder(DefaultPathNextTo(modPath), Entry(ModFolders.GameSetsName, Globals.GameSet.Default)));
        foreach (var extension in extensions) folders.Add(new ArchiveFolder(extension.Folder, Entry(ModFolders.ExtensionsName, extension.FolderName)));
        if (texturePacks != null)
            foreach (var pack in texturePacks) folders.Add(new ArchiveFolder(pack.Folder, Entry(ModFolders.TexturePacksName, pack.FolderName)));
        return DirectoryArchive.Pack(folders);
    }

    private static string Entry(string folder, string name) => folder + "/" + name;

    private static string FolderName(string path)
    {
        return Path.GetFileName(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar));
    }
}
