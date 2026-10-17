using Engine.Graphics.Atlas;

namespace Spacebox.Game.GUI;

public static class UIAtlas
{
    private const string Folder = "Resources/Textures/UI";
    private const string Prefix = "ui/";
    private const int MaxElementSize = 128;

    private static TextureAtlas _standalone;

    public static void AddUIElements(TextureAtlas atlas)
    {
        if (!Directory.Exists(Folder)) return;

        var files = Directory.GetFiles(Folder, "*.*", SearchOption.AllDirectories);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!IsImage(file)) continue;
            if (!ImagePixels.TryGetSize(file, out int width, out int height)) continue;
            if (width > MaxElementSize || height > MaxElementSize) continue;

            atlas.AddFile(ToName(file), file);
        }
    }

    public static AtlasSprite Get(string name)
    {
        string key = Prefix + name;

        var items = GameAssets.AtlasItems;
        if (items != null && items.Contains(key)) return AtlasSprite.From(items, key);

        return AtlasSprite.From(GetStandalone(), key);
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
