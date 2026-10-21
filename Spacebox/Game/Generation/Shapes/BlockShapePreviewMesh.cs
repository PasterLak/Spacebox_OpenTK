using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockShapePreviewMesh
{
    private const int FloatsPerVertex = 8;
    private static readonly Vector3 CellCenter = new Vector3(0.5f);
    private static readonly List<MultiblockCell> Cells = new List<MultiblockCell>();

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, BlockShapePose pose, BlockState state)
    {
        Cells.Clear();
        Cells.Add(new MultiblockCell(Vector3i.Zero, new Vector3Byte(0)));
        return Build(data, shape, pose, state, Cells);
    }

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, Direction direction, Rotation rotation, bool supported, BlockState state)
    {
        var pose = shape.GetPose(direction, rotation, supported);
        if (!data.IsMultiblock) return Build(data, shape, pose, state);

        MultiblockLayout.GetCells(data, direction, rotation, Cells);
        return Build(data, shape, pose, state, Cells);
    }

    private static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, BlockShapePose pose, BlockState state, List<MultiblockCell> cells)
    {
        var geometry = shape.Geometry;
        int vertexCount = geometry.VertexCount;
        var vertices = new float[vertexCount * cells.Count * FloatsPerVertex];
        var indices = new uint[geometry.Indices.Length * cells.Count];

        for (int c = 0; c < cells.Count; c++)
        {
            var cell = cells[c];
            var offset = new Vector3(cell.Offset.X, cell.Offset.Y, cell.Offset.Z);

            for (int i = 0; i < vertexCount; i++)
            {
                var position = pose.Positions[i] - CellCenter + offset;
                var normal = pose.Normals[i];
                var face = shape.VertexFaces[i];
                var faceUVs = data.GetFaceUV(face, state) ?? BlockShapeTexturing.DefaultFaceUVs;
                if (data.IsMultiblock)
                    faceUVs = MultiblockLayout.GetFaceUVs(faceUVs, data.GetFaceTiles(face, state), data.Size, cell.Model, (Direction)face);
                var uv = BlockShapeTexturing.MapToFace(faceUVs, geometry.TexCoords[i]);

                int o = (c * vertexCount + i) * FloatsPerVertex;
                vertices[o] = position.X;
                vertices[o + 1] = position.Y;
                vertices[o + 2] = position.Z;
                vertices[o + 3] = normal.X;
                vertices[o + 4] = normal.Y;
                vertices[o + 5] = normal.Z;
                vertices[o + 6] = uv.X;
                vertices[o + 7] = uv.Y;
            }

            int baseIndex = c * geometry.Indices.Length;
            for (int i = 0; i < geometry.Indices.Length; i++)
                indices[baseIndex + i] = (uint)(c * vertexCount + geometry.Indices[i]);
        }

        return (vertices, indices);
    }
}
