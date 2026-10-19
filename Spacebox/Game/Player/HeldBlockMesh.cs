using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player;

public static class HeldBlockMesh
{
    public const int FloatsPerVertex = 8;

    public static bool TryBuild(BlockData data, out float[] vertices, out uint[] indices)
    {
        vertices = null;
        indices = null;
        if (data == null) return false;

        if (data.Shape != null)
        {
            var pose = data.Shape.GetPose(Block.DefaultDirection, Rotation.None, true);
            (vertices, indices) = BlockShapePreviewMesh.Build(data, data.Shape, pose, BlockState.Inactive);
        }
        else if (data.IsMultiblock)
        {
            (vertices, indices) = MultiblockPreviewMesh.Build(data, BlockState.Inactive);
        }
        else
        {
            return false;
        }

        if (vertices.Length == 0 || indices.Length == 0) return false;

        FitToUnitCube(vertices, FloatsPerVertex);
        return true;
    }

    public static void FitToUnitCube(float[] vertices, int stride)
    {
        if (vertices == null || stride < 3 || vertices.Length < stride) return;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (int i = 0; i + 2 < vertices.Length; i += stride)
        {
            var position = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
            min = Vector3.ComponentMin(min, position);
            max = Vector3.ComponentMax(max, position);
        }

        var size = max - min;
        float extent = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        bool outside = min.X < -0.5f || min.Y < -0.5f || min.Z < -0.5f || max.X > 0.5f || max.Y > 0.5f || max.Z > 0.5f;
        if (extent <= 1f && !outside) return;

        float scale = extent > 1f ? 1f / extent : 1f;
        var center = (min + max) * 0.5f;
        for (int i = 0; i + 2 < vertices.Length; i += stride)
        {
            vertices[i] = (vertices[i] - center.X) * scale;
            vertices[i + 1] = (vertices[i + 1] - center.Y) * scale;
            vertices[i + 2] = (vertices[i + 2] - center.Z) * scale;
        }
    }
}
