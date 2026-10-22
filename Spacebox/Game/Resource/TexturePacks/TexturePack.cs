using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource.TexturePacks;

public sealed class TexturePack : ITextureOverrideSource
{
    public const string InfoFile = "pack.json";
    public const string IconFile = "icon.png";
    public const string UIFolder = "UI";

    private readonly Dictionary<string, string> _blockTextures;
    private readonly Dictionary<string, string> _itemTextures;
    private readonly Dictionary<string, string> _emissions;
    private readonly Dictionary<string, string> _ui;

    public string Folder { get; }
    public TexturePackInfo Info { get; }

    public string Id => string.IsNullOrWhiteSpace(Info.Id) ? FolderName : Info.Id;
    public string FolderName => Path.GetFileName(Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    public string DisplayName => string.IsNullOrWhiteSpace(Info.Name) ? FolderName : Info.Name;
    public string IconPath => Path.Combine(Folder, IconFile);

    public IReadOnlyDictionary<string, string> BlockTextures => _blockTextures;
    public IReadOnlyDictionary<string, string> ItemTextures => _itemTextures;
    public IReadOnlyDictionary<string, string> Emissions => _emissions;
    public IReadOnlyDictionary<string, string> UI => _ui;

    private TexturePack(string folder, TexturePackInfo info)
    {
        Folder = folder;
        Info = info ?? new TexturePackInfo();
        _blockTextures = AssetFolderScanner.Scan(Path.Combine(folder, GameSetFormat.BlocksFolder), GameSetFormat.ImageExtensions, null, false);
        _itemTextures = AssetFolderScanner.Scan(Path.Combine(folder, GameSetFormat.ItemsFolder), GameSetFormat.ImageExtensions, null, false);
        _emissions = AssetFolderScanner.Scan(Path.Combine(folder, GameSetFormat.EmissionsFolder), GameSetFormat.ImageExtensions, null, true);
        _ui = ScanUI(Path.Combine(folder, UIFolder));
    }

    public static TexturePack Load(string folder)
    {
        if (!Directory.Exists(folder)) return null;

        string infoPath = Path.Combine(folder, InfoFile);
        TexturePackInfo info = null;
        if (File.Exists(infoPath))
        {
            var node = ContentJson.ParseFile(infoPath, null);
            if (node != null)
            {
                try
                {
                    info = System.Text.Json.JsonSerializer.Deserialize<TexturePackInfo>(node, ContentJson.CaseInsensitive);
                }
                catch (System.Text.Json.JsonException)
                {
                    info = null;
                }
            }
        }

        return new TexturePack(folder, info);
    }

    public bool Matches(string idOrFolder)
    {
        return string.Equals(Id, idOrFolder, StringComparison.OrdinalIgnoreCase) || string.Equals(FolderName, idOrFolder, StringComparison.OrdinalIgnoreCase);
    }

    public int TextureCount => _blockTextures.Count + _itemTextures.Count + _emissions.Count + _ui.Count;

    public static string UIKey(string relativePathWithoutExtension)
    {
        return relativePathWithoutExtension.Replace('\\', '/').ToLowerInvariant();
    }

    private static Dictionary<string, string> ScanUI(string root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(root)) return result;

        var files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (!GameSetFormat.HasExtension(file, GameSetFormat.ImageExtensions)) continue;
            string key = UIKey(Path.ChangeExtension(Path.GetRelativePath(root, file), null));
            result.TryAdd(key, file);
        }
        return result;
    }
}
