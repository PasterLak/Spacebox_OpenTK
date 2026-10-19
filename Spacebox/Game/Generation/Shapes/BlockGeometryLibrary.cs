using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockGeometryLibrary
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, BlockGeometry> Geometries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slab"] = new BlockGeometryBuilder().AddBox(new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)).Build()
    };

    public static void Register(string name, BlockGeometry geometry)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Geometry name is empty", nameof(name));
        ArgumentNullException.ThrowIfNull(geometry);

        lock (Sync) Geometries[name] = geometry;
    }

    public static bool TryGet(string name, [NotNullWhen(true)] out BlockGeometry? geometry)
    {
        geometry = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        lock (Sync) return Geometries.TryGetValue(name, out geometry);
    }
}
