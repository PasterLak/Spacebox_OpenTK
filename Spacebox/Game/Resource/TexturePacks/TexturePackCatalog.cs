namespace Spacebox.Game.Resource.TexturePacks;

public static class TexturePackCatalog
{
    public static List<TexturePack> Scan(string directory)
    {
        var packs = new List<TexturePack>();
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return packs;

        var folders = Directory.GetDirectories(directory);
        Array.Sort(folders, StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            if (Path.GetFileName(folder).StartsWith('_') || ModFolderInstaller.IsTransient(folder)) continue;
            var pack = TexturePack.Load(folder);
            if (pack != null) packs.Add(pack);
        }
        return packs;
    }

    public static List<TexturePack> Active(IReadOnlyList<TexturePack> available, IReadOnlyList<string> enabled)
    {
        var result = new List<TexturePack>();
        if (enabled == null) return result;

        foreach (var id in enabled)
        {
            foreach (var pack in available)
            {
                if (pack.Matches(id) && !result.Contains(pack))
                {
                    result.Add(pack);
                    break;
                }
            }
        }
        return result;
    }

    public static List<TexturePack> WithSessionPacks(List<TexturePack> active, IReadOnlyList<TexturePack> sessionPacks)
    {
        if (sessionPacks == null) return active;

        foreach (var pack in sessionPacks)
        {
            if (!active.Exists(existing => existing.Matches(pack.Id))) active.Add(pack);
        }
        return active;
    }
}
