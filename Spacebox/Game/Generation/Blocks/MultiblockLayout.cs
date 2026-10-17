using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public readonly struct MultiblockCell
{
    public readonly Vector3i Offset;
    public readonly Vector3Byte Model;

    public MultiblockCell(Vector3i offset, Vector3Byte model)
    {
        Offset = offset;
        Model = model;
    }

    public bool IsOrigin => Model.X == 0 && Model.Y == 0 && Model.Z == 0;
}

public static class MultiblockLayout
{
    public const int MaxSize = 16;

    public static Matrix4 GetTransform(BlockData data, Direction direction, Rotation rotation)
    {
        return BlockRotationHelper.CalculateTransformMatrix(data.BaseFrontDirection, direction, rotation);
    }

    public static Vector3i ToEntityOffset(Vector3Byte model, Matrix4 transform)
    {
        return ToEntityOffset(new Vector3(model.X, model.Y, model.Z), transform);
    }

    public static Vector3i ToEntityOffset(Vector3 model, Matrix4 transform)
    {
        var rotated = Vector3.TransformVector(model, transform);
        return new Vector3i((int)MathF.Round(rotated.X), (int)MathF.Round(rotated.Y), (int)MathF.Round(rotated.Z));
    }

    public static void GetCells(BlockData data, Direction direction, Rotation rotation, List<MultiblockCell> cells)
    {
        cells.Clear();
        var size = data.Size;
        var transform = GetTransform(data, direction, rotation);

        for (byte x = 0; x < size.X; x++)
            for (byte y = 0; y < size.Y; y++)
                for (byte z = 0; z < size.Z; z++)
                {
                    var model = new Vector3Byte(x, y, z);
                    cells.Add(new MultiblockCell(ToEntityOffset(model, transform), model));
                }
    }

    public static Vector3i GetOriginOffset(BlockData data, Block cellBlock)
    {
        var transform = GetTransform(data, cellBlock.Direction, cellBlock.Rotation);
        return ToEntityOffset(cellBlock.MultiblockOffset, transform);
    }

    public static void GetModelFaceAxes(Direction face, out Vector3 normal, out Vector3 up, out Vector3 right)
    {
        switch (face)
        {
            case Direction.Forward:
                normal = Vector3.UnitZ; up = Vector3.UnitY; right = Vector3.UnitX;
                break;
            case Direction.Back:
                normal = -Vector3.UnitZ; up = Vector3.UnitY; right = -Vector3.UnitX;
                break;
            case Direction.Left:
                normal = -Vector3.UnitX; up = Vector3.UnitY; right = Vector3.UnitZ;
                break;
            case Direction.Right:
                normal = Vector3.UnitX; up = Vector3.UnitY; right = -Vector3.UnitZ;
                break;
            case Direction.Up:
                normal = Vector3.UnitY; up = -Vector3.UnitZ; right = Vector3.UnitX;
                break;
            default:
                normal = -Vector3.UnitY; up = Vector3.UnitZ; right = Vector3.UnitX;
                break;
        }
    }

    public static Vector2[] GetFaceUVs(Vector2[] baseUvs, Vector2i tiles, Vector3Byte size, Vector3Byte cell, Direction face)
    {
        GetFaceGrid(size, cell, face, out int column, out int columns, out int row, out int rows);

        int tilesX = Math.Clamp(tiles.X, 1, columns);
        int tilesY = Math.Clamp(tiles.Y, 1, rows);
        int tileColumn = column % tilesX;
        int tileRow = row % tilesY;

        var min = baseUvs[0];
        var max = baseUvs[2];
        float u0 = MathHelper.Lerp(min.X, max.X, tileColumn / (float)tilesX);
        float u1 = MathHelper.Lerp(min.X, max.X, (tileColumn + 1) / (float)tilesX);
        float v0 = MathHelper.Lerp(min.Y, max.Y, tileRow / (float)tilesY);
        float v1 = MathHelper.Lerp(min.Y, max.Y, (tileRow + 1) / (float)tilesY);

        return new[]
        {
            new Vector2(u0, v0),
            new Vector2(u1, v0),
            new Vector2(u1, v1),
            new Vector2(u0, v1)
        };
    }

    private static void GetFaceGrid(Vector3Byte size, Vector3Byte cell, Direction face, out int column, out int columns, out int row, out int rows)
    {
        switch (face)
        {
            case Direction.Forward:
                column = cell.X; columns = size.X; row = cell.Y; rows = size.Y;
                break;
            case Direction.Back:
                column = size.X - 1 - cell.X; columns = size.X; row = cell.Y; rows = size.Y;
                break;
            case Direction.Left:
                column = cell.Z; columns = size.Z; row = cell.Y; rows = size.Y;
                break;
            case Direction.Right:
                column = size.Z - 1 - cell.Z; columns = size.Z; row = cell.Y; rows = size.Y;
                break;
            case Direction.Up:
                column = cell.X; columns = size.X; row = size.Z - 1 - cell.Z; rows = size.Z;
                break;
            default:
                column = cell.X; columns = size.X; row = cell.Z; rows = size.Z;
                break;
        }
    }
}
