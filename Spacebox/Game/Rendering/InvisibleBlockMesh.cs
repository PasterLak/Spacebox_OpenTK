using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Rendering;

public static class InvisibleBlockMesh
{
    public const int FloatsPerVertex = 8;

    private static readonly Face[] Faces = (Face[])Enum.GetValues(typeof(Face));

    public static void AddCell(Vector3i cell, BlockData data, Func<Vector3i, bool> hidesFace, List<float> vertices, List<uint> indices)
    {
        for (int f = 0; f < Faces.Length; f++)
        {
            var face = Faces[f];
            var normal = face.GetNormal();
            if (hidesFace(new Vector3i(cell.X + normal.X, cell.Y + normal.Y, cell.Z + normal.Z))) continue;

            var uvs = data?.GetFaceUV(face) ?? CubeMeshData.GetBasicUVs();
            if (uvs.Length < 4) uvs = CubeMeshData.GetBasicUVs();
            var corners = CubeMeshData.GetFaceVertices(face);
            uint start = (uint)(vertices.Count / FloatsPerVertex);

            for (int i = 0; i < 4; i++)
            {
                vertices.Add(cell.X + corners[i].X);
                vertices.Add(cell.Y + corners[i].Y);
                vertices.Add(cell.Z + corners[i].Z);
                vertices.Add(normal.X);
                vertices.Add(normal.Y);
                vertices.Add(normal.Z);
                vertices.Add(uvs[i].X);
                vertices.Add(uvs[i].Y);
            }

            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start + 2);
            indices.Add(start + 3);
            indices.Add(start);
        }
    }
}
