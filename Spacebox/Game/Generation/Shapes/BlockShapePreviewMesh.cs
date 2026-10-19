using OpenTK.Mathematics;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockShapePreviewMesh
{
    private const int FloatsPerVertex = 8;
    private static readonly Vector3 CellCenter = new Vector3(0.5f);

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, BlockShapePose pose, BlockState state)
    {
        var geometry = shape.Geometry;
        var vertices = new float[geometry.VertexCount * FloatsPerVertex];

        for (int i = 0; i < geometry.VertexCount; i++)
        {
            var position = pose.Positions[i] - CellCenter;
            var normal = pose.Normals[i];
            var faceUVs = data.GetFaceUV(shape.VertexFaces[i], state) ?? BlockShapeTexturing.DefaultFaceUVs;
            var uv = BlockShapeTexturing.MapToFace(faceUVs, geometry.TexCoords[i]);

            int o = i * FloatsPerVertex;
            vertices[o] = position.X;
            vertices[o + 1] = position.Y;
            vertices[o + 2] = position.Z;
            vertices[o + 3] = normal.X;
            vertices[o + 4] = normal.Y;
            vertices[o + 5] = normal.Z;
            vertices[o + 6] = uv.X;
            vertices[o + 7] = uv.Y;
        }

        var indices = new uint[geometry.Indices.Length];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = (uint)geometry.Indices[i];

        return (vertices, indices);
    }
}
