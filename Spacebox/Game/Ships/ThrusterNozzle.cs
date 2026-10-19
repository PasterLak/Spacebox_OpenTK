using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Ships;

public static class ThrusterNozzle
{
    public static void GetCells(BlockData data, int length, Direction direction, Rotation rotation, List<Vector3i> offsets)
    {
        offsets.Clear();
        if (length <= 0) return;

        var transform = MultiblockLayout.GetTransform(data, direction, rotation);
        var frontVector = BlockRotationHelper.GetDirectionVector(data.BaseFrontDirection);
        var front = new Vector3i((int)MathF.Round(frontVector.X), (int)MathF.Round(frontVector.Y), (int)MathF.Round(frontVector.Z));
        var size = new Vector3i(data.Size.X, data.Size.Y, data.Size.Z);

        for (int x = 0; x < size.X; x++)
        for (int y = 0; y < size.Y; y++)
        for (int z = 0; z < size.Z; z++)
        {
            var model = new Vector3i(x, y, z);
            if (IsInside(model + front, size)) continue;

            for (int step = 1; step <= length; step++)
            {
                var cell = model + front * step;
                offsets.Add(MultiblockLayout.ToEntityOffset(new Vector3(cell.X, cell.Y, cell.Z), transform));
            }
        }
    }

    private static bool IsInside(Vector3i cell, Vector3i size)
    {
        return cell.X >= 0 && cell.Y >= 0 && cell.Z >= 0 && cell.X < size.X && cell.Y < size.Y && cell.Z < size.Z;
    }
}
