using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.Resource.TexturePacks;

public static class TexturePackService
{
    public static readonly string FolderName = Path.Combine(ModFolders.RootName, ModFolders.TexturePacksName);

    private static List<TexturePack> _available;
    private static IReadOnlyList<TexturePack> _sessionPacks = Array.Empty<TexturePack>();

    public static string Directory => ModFolders.Local.TexturePacks;

    public static IReadOnlyList<TexturePack> Available => _available ??= TexturePackCatalog.Scan(Directory);

    public static List<string> Enabled => Settings.TexturePacks.Enabled;

    public static void Rescan()
    {
        _available = null;
        Spacebox.Game.GUI.UIAtlas.Invalidate();
    }

    public static IReadOnlyList<TexturePack> SessionPacks => _sessionPacks;

    public static IReadOnlyList<TexturePack> Active()
    {
        return TexturePackCatalog.WithSessionPacks(TexturePackCatalog.Active(Available, Enabled), _sessionPacks);
    }

    public static void SetSessionPacks(IReadOnlyList<string> folders)
    {
        var packs = new List<TexturePack>();
        if (folders != null)
        {
            foreach (var folder in folders)
            {
                var pack = TexturePack.Load(folder);
                if (pack != null) packs.Add(pack);
            }
        }

        _sessionPacks = packs;
        Changed();
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
