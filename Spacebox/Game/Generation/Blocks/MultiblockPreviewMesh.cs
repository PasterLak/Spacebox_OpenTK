using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public static class MultiblockPreviewMesh
{
    private static readonly Direction[] _faces =
    {
        Direction.Down, Direction.Up, Direction.Left, Direction.Right, Direction.Back, Direction.Forward
    };

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockState state)
    {
        var size = data.Size;
        var vertices = new List<float>();
        var indices = new List<uint>();
        uint vertexIndex = 0;

        for (byte x = 0; x < size.X; x++)
            for (byte y = 0; y < size.Y; y++)
                for (byte z = 0; z < size.Z; z++)
                {
                    var cell = new Vector3Byte(x, y, z);
                    var center = new Vector3(x, y, z);

                    foreach (var face in _faces)
                    {
                        MultiblockLayout.GetModelFaceAxes(face, out var normal, out var up, out var right);
                        if (IsInside(size, center + normal)) continue;

                        var baseUvs = data.GetFaceUV((Face)face, state);
                        if (baseUvs == null) continue;

                        var uvs = MultiblockLayout.GetFaceUVs(baseUvs, data.GetFaceTiles((Face)face, state), size, cell, face);
                        var faceCenter = center + normal * 0.5f;
                        var halfUp = up * 0.5f;
                        var halfRight = right * 0.5f;

                        AddVertex(vertices, faceCenter - halfUp - halfRight, normal, uvs[0]);
                        AddVertex(vertices, faceCenter - halfUp + halfRight, normal, uvs[1]);
                        AddVertex(vertices, faceCenter + halfUp + halfRight, normal, uvs[2]);
                        AddVertex(vertices, faceCenter + halfUp - halfRight, normal, uvs[3]);

                        indices.Add(vertexIndex);
                        indices.Add(vertexIndex + 1);
                        indices.Add(vertexIndex + 2);
                        indices.Add(vertexIndex + 2);
                        indices.Add(vertexIndex + 3);
                        indices.Add(vertexIndex);
                        vertexIndex += 4;
                    }
                }

        return (vertices.ToArray(), indices.ToArray());
    }

    private static bool IsInside(Vector3Byte size, Vector3 position)
    {
        return position.X >= 0 && position.X < size.X &&
               position.Y >= 0 && position.Y < size.Y &&
               position.Z >= 0 && position.Z < size.Z;
    }

    private static void AddVertex(List<float> vertices, Vector3 position, Vector3 normal, Vector2 uv)
    {
        vertices.Add(position.X);
        vertices.Add(position.Y);
        vertices.Add(position.Z);
        vertices.Add(normal.X);
        vertices.Add(normal.Y);
        vertices.Add(normal.Z);
        vertices.Add(uv.X);
        vertices.Add(uv.Y);
    }
}
