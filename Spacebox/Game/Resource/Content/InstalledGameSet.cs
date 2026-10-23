using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.Resource.Content;

public sealed class InstalledGameSet
{
    public string Folder { get; }
    public ModConfig Config { get; }

    public InstalledGameSet(string folder, ModConfig config)
    {
        Folder = folder;
        Config = config ?? new ModConfig();
    }

    public string Id => Config.ModId ?? "";
    public string FolderName => Path.GetFileName(Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    public string DisplayName => string.IsNullOrWhiteSpace(Config.ModName) ? Id : Config.ModName;
    public bool IsExtension => GameSetRoles.IsExtension(Config.Extends);
    public bool IsBasedOnEmpty => GameSetRoles.IsBasedOnEmpty(Config.Extends);
    public bool IsEmpty => GameSetRoles.IsEmpty(Id);
    public IReadOnlyList<string> Includes => Config.Includes ?? new List<string>();

    public bool Matches(string idOrFolder)
    {
        if (string.IsNullOrWhiteSpace(idOrFolder)) return false;
        return string.Equals(Id, idOrFolder, StringComparison.OrdinalIgnoreCase) || string.Equals(FolderName, idOrFolder, StringComparison.OrdinalIgnoreCase);
    }

    public ContentLayer ToLayer() => new ContentLayer(Id, Folder);

    public override string ToString() => $"{Id} ({FolderName})";
}
