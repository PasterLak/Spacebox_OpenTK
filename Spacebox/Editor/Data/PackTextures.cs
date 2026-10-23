using Spacebox.Game.Resource.Content;

namespace Spacebox.Editor.Data;

public enum PackTextureKind
{
    Block,
    Item,
    Glow
}

public sealed record PackTextureRow(PackTextureKind Kind, string Name, string BasePath, string PackPath)
{
    public bool InPack => PackPath != null;
    public bool Unused => BasePath == null;
}

public static class PackTextures
{
    public const string DefaultExtension = ".png";

    public static string FolderOf(PackTextureKind kind) => kind switch
    {
        PackTextureKind.Block => GameSetFormat.BlocksFolder,
        PackTextureKind.Item => GameSetFormat.ItemsFolder,
        _ => GameSetFormat.EmissionsFolder
    };

    public static List<PackTextureRow> Build(GameSetAssetIndex baseIndex, ITextureOverrideSource pack)
    {
        var rows = new List<PackTextureRow>();
        Add(rows, PackTextureKind.Block, baseIndex?.BlockTextures, pack?.BlockTextures);
        Add(rows, PackTextureKind.Item, baseIndex?.ItemTextures, pack?.ItemTextures);

        var glow = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (baseIndex != null)
        {
            AssetFolderScanner.Fill(glow, baseIndex.BlockEmissions);
            AssetFolderScanner.Fill(glow, baseIndex.ItemEmissions);
        }
        Add(rows, PackTextureKind.Glow, glow, pack?.Emissions);
        return rows;
    }

    public static string TargetPath(string packFolder, PackTextureRow row)
    {
        string extension = row.BasePath != null ? Path.GetExtension(row.BasePath).ToLowerInvariant() : DefaultExtension;
        return Path.Combine(packFolder, FolderOf(row.Kind), row.Name + extension);
    }

    private static void Add(List<PackTextureRow> rows, PackTextureKind kind, IReadOnlyDictionary<string, string> baseTextures, IReadOnlyDictionary<string, string> packTextures)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (baseTextures != null) names.UnionWith(baseTextures.Keys);
        if (packTextures != null) names.UnionWith(packTextures.Keys);

        foreach (var name in names)
        {
            string basePath = baseTextures != null && baseTextures.TryGetValue(name, out var b) ? b : null;
            string packPath = packTextures != null && packTextures.TryGetValue(name, out var p) ? p : null;
            rows.Add(new PackTextureRow(kind, name, basePath, packPath));
        }
    }
}
