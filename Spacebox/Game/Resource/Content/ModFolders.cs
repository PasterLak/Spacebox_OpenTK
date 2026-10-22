namespace Spacebox.Game.Resource.Content;

public sealed class ModFolders
{
    public const string RootName = "Mods";
    public const string GameSetsName = "GameSets";
    public const string ExtensionsName = "Extensions";
    public const string TexturePacksName = "TexturePacks";

    public ModFolders(string root) : this(Path.Combine(root, GameSetsName), Path.Combine(root, ExtensionsName), Path.Combine(root, TexturePacksName))
    {
    }

    private ModFolders(string gameSets, string extensions, string texturePacks)
    {
        GameSets = gameSets;
        Extensions = extensions;
        TexturePacks = texturePacks;
    }

    public static ModFolders Local => new ModFolders(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RootName));

    public static ModFolders Flat(string folder) => new ModFolders(folder, folder, folder);

    public string GameSets { get; }
    public string Extensions { get; }
    public string TexturePacks { get; }
    public string DefaultGameSet => Path.Combine(GameSets, Globals.GameSet.Default);

    public IReadOnlyList<string> ContentFolders => string.Equals(Normalize(GameSets), Normalize(Extensions), StringComparison.OrdinalIgnoreCase)
        ? new[] { GameSets }
        : new[] { GameSets, Extensions };

    public string FolderFor(bool isExtension) => isExtension ? Extensions : GameSets;

    private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
