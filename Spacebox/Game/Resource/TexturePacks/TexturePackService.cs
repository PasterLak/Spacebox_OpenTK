using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource.TexturePacks;

public static class TexturePackService
{
    public const string FolderName = "TexturePacks";

    private static List<TexturePack> _available;

    public static string Directory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FolderName);

    public static IReadOnlyList<TexturePack> Available => _available ??= TexturePackCatalog.Scan(Directory);

    public static List<string> Enabled => Settings.TexturePacks.Enabled;

    public static void Rescan()
    {
        _available = null;
        Spacebox.Game.GUI.UIAtlas.Invalidate();
    }

    public static IReadOnlyList<TexturePack> Active()
    {
        return TexturePackCatalog.Active(Available, Enabled);
    }

    public static IReadOnlyList<ITextureOverrideSource> ActiveOverrides()
    {
        var active = Active();
        var result = new List<ITextureOverrideSource>(active.Count);
        foreach (var pack in active) result.Add(pack);
        return result;
    }

    public static string ResolveUI(string key)
    {
        foreach (var pack in Active())
        {
            if (pack.UI.TryGetValue(key, out var path)) return path;
        }
        return null;
    }

    public static void Changed()
    {
        Spacebox.Game.GUI.UIAtlas.Invalidate();
    }
}
