using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Shapes;

public static class BlockShapePreviewMesh
{
    private const int FloatsPerVertex = 8;
    private static readonly Vector3 CellCenter = new Vector3(0.5f);
    private static readonly List<MultiblockCell> Cells = new List<MultiblockCell>();
    private static readonly List<BlockShape> CellShapes = new List<BlockShape>();
    private static readonly List<BlockShapePose> CellPoses = new List<BlockShapePose>();

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, BlockShapePose pose, BlockState state)
    {
        Cells.Clear();
        CellShapes.Clear();
        CellPoses.Clear();
        Cells.Add(new MultiblockCell(Vector3i.Zero, new Vector3Byte(0)));
        CellShapes.Add(shape);
        CellPoses.Add(pose);
        return Build(data, state);
    }

    public static (float[] vertices, uint[] indices) Build(BlockData data, BlockShape shape, Direction direction, Rotation rotation, bool supported, BlockState state)
    {
        if (!data.IsMultiblock) return Build(data, shape, shape.GetPose(direction, rotation, supported), state);

        MultiblockLayout.GetCells(data, direction, rotation, Cells);
        CellShapes.Clear();
        CellPoses.Clear();
        for (int c = 0; c < Cells.Count; c++)
        {
            var cellShape = shape.ForCell(data.Size, Cells[c].Model);
            CellShapes.Add(cellShape);
            CellPoses.Add(cellShape.GetPose(direction, rotation, supported));
        }
        return Build(data, state);
    }

    private static (float[] vertices, uint[] indices) Build(BlockData data, BlockState state)
    {
        int totalVertices = 0;
        int totalIndices = 0;
        for (int c = 0; c < Cells.Count; c++)
        {
            totalVertices += CellShapes[c].Geometry.VertexCount;
            totalIndices += CellShapes[c].Geometry.Indices.Length;
        }

        var vertices = new float[totalVertices * FloatsPerVertex];
        var indices = new uint[totalIndices];
        int vertexBase = 0;
        int indexBase = 0;

        for (int c = 0; c < Cells.Count; c++)
        {
            var cell = Cells[c];
            var shape = CellShapes[c];
            var pose = CellPoses[c];
            var geometry = shape.Geometry;
            int vertexCount = geometry.VertexCount;
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

                int o = (vertexBase + i) * FloatsPerVertex;
                vertices[o] = position.X;
                vertices[o + 1] = position.Y;
                vertices[o + 2] = position.Z;
                vertices[o + 3] = normal.X;
                vertices[o + 4] = normal.Y;
                vertices[o + 5] = normal.Z;
                vertices[o + 6] = uv.X;
                vertices[o + 7] = uv.Y;
            }

            for (int i = 0; i < geometry.Indices.Length; i++)
                indices[indexBase + i] = (uint)(vertexBase + geometry.Indices[i]);

            vertexBase += vertexCount;
            indexBase += geometry.Indices.Length;
        }

        return (vertices, indices);
    }
}
