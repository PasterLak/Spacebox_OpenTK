using Engine;

namespace Spacebox.Game.Generation.Shapes;

public sealed class BlockShapeResolver
{
    public const string ModelsFolder = "Models/Blocks";
    public const string ModelExtension = ".obj";

    private readonly string[] _searchRoots;
    private readonly Dictionary<string, BlockShape?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public BlockShapeResolver(params string[] searchRoots)
    {
        _searchRoots = searchRoots.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToArray();
    }

    public BlockShape? Resolve(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return null;
        model = model.Trim();

        if (_cache.TryGetValue(model, out var cached)) return cached;

        var shape = CreateShape(model);
        _cache[model] = shape;
        return shape;
    }

    private BlockShape? CreateShape(string model)
    {
        if (BlockGeometryLibrary.TryGet(model, out var geometry))
            return new BlockShape(geometry);

        var path = FindModelFile(model);
        if (path == null)
        {
            Debug.Error($"[BlockShapeResolver] Block model '{model}' was not found, the block uses a cube");
            return null;
        }

        try
        {
            return new BlockShape(ObjBlockGeometry.Load(path));
        }
        catch (Exception ex)
        {
            Debug.Error($"[BlockShapeResolver] Failed to load block model '{path}': {ex.Message}");
            return null;
        }
    }

    private string? FindModelFile(string model)
    {
        string relative = Path.HasExtension(model) ? model : Path.Combine(ModelsFolder, model + ModelExtension);

        foreach (var root in _searchRoots)
        {
            string path = Path.Combine(root, relative);
            if (File.Exists(path)) return path;
        }

        return null;
    }
}
