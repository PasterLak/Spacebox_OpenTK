using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Building;

public readonly record struct ClipboardCell(Vector3i Offset, short BlockId, Direction Direction, Rotation Rotation);

public sealed class BlockClipboard
{
    public const int MaxCells = 8192;
    public const int MaxSize = 128;

    private readonly List<ClipboardCell> _cells;

    public BlockClipboard(Vector3i size, List<ClipboardCell> cells)
    {
        Size = size;
        _cells = cells;
    }

    public Vector3i Size { get; private set; }
    public IReadOnlyList<ClipboardCell> Cells => _cells;
    public int Count => _cells.Count;

    public void Turn(Func<short, Direction> baseFrontOf)
    {
        var size = Size;
        for (int i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            BlockTurn.Turn(baseFrontOf(cell.BlockId), cell.Direction, cell.Rotation, out var direction, out var rotation);
            _cells[i] = new ClipboardCell(BlockTurn.TurnOffset(cell.Offset, size), cell.BlockId, direction, rotation);
        }
        Size = BlockTurn.TurnSize(size);
    }

    public static Vector3i PasteMin(Vector3i anchor, Vector3i normal, Vector3i size)
    {
        var min = new Vector3i(anchor.X - size.X / 2, anchor.Y - size.Y / 2, anchor.Z - size.Z / 2);
        if (normal.X > 0) min.X = anchor.X;
        else if (normal.X < 0) min.X = anchor.X - size.X + 1;
        if (normal.Y > 0) min.Y = anchor.Y;
        else if (normal.Y < 0) min.Y = anchor.Y - size.Y + 1;
        if (normal.Z > 0) min.Z = anchor.Z;
        else if (normal.Z < 0) min.Z = anchor.Z - size.Z + 1;
        return min;
    }

    public static bool IsValidSize(Vector3i size)
    {
        return size.X > 0 && size.Y > 0 && size.Z > 0 && size.X <= MaxSize && size.Y <= MaxSize && size.Z <= MaxSize;
    }
}
