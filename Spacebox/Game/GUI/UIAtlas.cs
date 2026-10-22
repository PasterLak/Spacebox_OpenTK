using Engine.Graphics.Atlas;

namespace Spacebox.Game.GUI;

public static class UIAtlas
{
    private const string Folder = "Resources/Textures/UI";
    private const string Prefix = "ui/";
    private const int MaxElementSize = 128;

    private static TextureAtlas _standalone;

    public static void AddUIElements(TextureAtlas atlas, bool overwrite = false)
    {
        if (!Directory.Exists(Folder)) return;

        var files = Directory.GetFiles(Folder, "*.*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!IsImage(file)) continue;
            if (!ImagePixels.TryGetSize(file, out int width, out int height)) continue;
            if (width > MaxElementSize || height > MaxElementSize) continue;

            string name = ToName(file);
            atlas.AddFile(name, Spacebox.Game.Resource.TexturePacks.TexturePackService.ResolveUI(name.Substring(Prefix.Length)) ?? file, overwrite);
        }
    }

    public static AtlasSprite Get(string name)
    {
        string key = Prefix + name;

        var items = GameAssets.AtlasItems;
        if (items != null && items.Contains(key)) return AtlasSprite.From(items, key);

        return AtlasSprite.From(GetStandalone(), key);
    }

    public static void Invalidate()
    {
        if (_standalone != null) AddUIElements(_standalone, true);
        if (GameAssets.AtlasItems != null) AddUIElements(GameAssets.AtlasItems, true);
    }

    private static TextureAtlas GetStandalone()
    {
        if (_standalone != null) return _standalone;

        _standalone = new TextureAtlas("UIAtlas", padding: 1);
        AddUIElements(_standalone);
        _standalone.Build();
        return _standalone;
    }

    private static string ToName(string file)
    {
        string relative = Path.GetRelativePath(Folder, file);
        string withoutExtension = Path.ChangeExtension(relative, null);
        return Prefix + withoutExtension.Replace('\\', '/').ToLowerInvariant();
    }

    private static bool IsImage(string file)
    {
        var extension = Path.GetExtension(file);
        foreach (var supported in ImagePixels.SupportedExtensions)
        {
            if (string.Equals(extension, supported, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
