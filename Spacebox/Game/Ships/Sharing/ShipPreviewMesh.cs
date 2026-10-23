using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Ships.Sharing;

public readonly record struct ShipPreviewCell(Vector3i Cell, int Kind);

public static class ShipPreviewMesh
{
    public const int FloatsPerVertex = 8;
    public const int MaxCells = 60000;

    private static readonly Face[] Faces = { Face.Down, Face.Up, Face.Left, Face.Right, Face.Back, Face.Forward };

    public static int ScaleFor(int cellCount)
    {
        int scale = 1;
        while (cellCount / (scale * scale * scale) > MaxCells) scale++;
        return scale;
    }

    public static IReadOnlyList<ShipPreviewCell> Downscale(IReadOnlyList<ShipPreviewCell> cells, int scale)
    {
        if (scale <= 1) return cells;
        var merged = new Dictionary<Vector3i, int>();
        foreach (var cell in cells)
        {
            var key = new Vector3i(FloorDiv(cell.Cell.X, scale), FloorDiv(cell.Cell.Y, scale), FloorDiv(cell.Cell.Z, scale));
            merged.TryAdd(key, cell.Kind);
        }

        var result = new List<ShipPreviewCell>(merged.Count);
        foreach (var pair in merged) result.Add(new ShipPreviewCell(pair.Key, pair.Value));
        return result;
    }

    public static (float[] Vertices, uint[] Indices) Build(IReadOnlyList<ShipPreviewCell> cells, Func<int, Face, Vector2[]> uvOf, int scale = 1)
    {
        var occupied = new HashSet<Vector3i>(cells.Count);
        foreach (var cell in cells) occupied.Add(cell.Cell);

        var vertices = new List<float>(cells.Count * 4 * FloatsPerVertex);
        var indices = new List<uint>(cells.Count * 6);
        uint vertex = 0;

        foreach (var cell in cells)
        {
            foreach (var face in Faces)
            {
                var normal = face.GetNormal();
                if (occupied.Contains(cell.Cell + new Vector3i(normal.X, normal.Y, normal.Z))) continue;

                var uvs = uvOf(cell.Kind, face);
                if (uvs == null || uvs.Length < 4) continue;

                var corners = CubeMeshData.GetFaceVertices(face);
                for (int i = 0; i < 4; i++)
                {
                    var position = ((Vector3)cell.Cell + corners[i]) * scale;
                    vertices.Add(position.X);
                    vertices.Add(position.Y);
                    vertices.Add(position.Z);
                    vertices.Add(normal.X);
                    vertices.Add(normal.Y);
                    vertices.Add(normal.Z);
                    vertices.Add(uvs[i].X);
                    vertices.Add(uvs[i].Y);
                }

                indices.Add(vertex);
                indices.Add(vertex + 1);
                indices.Add(vertex + 2);
                indices.Add(vertex + 2);
                indices.Add(vertex + 3);
                indices.Add(vertex);
                vertex += 4;
            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    private static int FloorDiv(int value, int divisor) => value >= 0 ? value / divisor : (value - divisor + 1) / divisor;
}
