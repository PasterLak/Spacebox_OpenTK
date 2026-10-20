using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource;

public class PipeJSON
{
    public float TransferInterval { get; set; } = 1f;
    public int Power { get; set; } = 5;
    public string[] OutputSides { get; set; } = { "Up", "Left", "Right", "Forward", "Back" };
    public string[] InputSides { get; set; } = { "Down" };
}

public class PipeBlockData : BlockData, IBlockTooltip
{
    public float TransferInterval { get; }
    public ushort Power { get; }
    public IReadOnlyList<Direction> OutputSides { get; }
    public IReadOnlyList<Direction> InputSides { get; }

    public PipeBlockData(BlockData baseBlock, PipeJSON settings) : base(baseBlock)
    {
        settings ??= new PipeJSON();
        TransferInterval = Math.Clamp(settings.TransferInterval, 0.05f, 60f);
        Power = (ushort)Math.Clamp(settings.Power, 0, 1000);
        InputSides = ParseSides(settings.InputSides, Array.Empty<Direction>(), new[] { Direction.Down });
        OutputSides = ParseSides(settings.OutputSides, InputSides, new[] { Direction.Up, Direction.Left, Direction.Right, Direction.Forward, Direction.Back });
    }

    public void AppendTooltip(System.Text.StringBuilder text)
    {
        text.Append("\nInputs: ").Append(SideNames(InputSides));
        text.Append("\nOutputs: ").Append(SideNames(OutputSides));
        text.Append($"\nMoves 1 item every {TransferInterval:0.##} s");
        text.Append($"\nPower usage: {Power} EU");
    }

    public static string SideNames(IReadOnlyList<Direction> sides)
    {
        var names = new string[sides.Count];
        for (int i = 0; i < sides.Count; i++) names[i] = SideName(sides[i]);
        return string.Join(", ", names);
    }

    private static string SideName(Direction side) => side switch
    {
        Direction.Up => "top",
        Direction.Down => "bottom",
        Direction.Forward => "front",
        Direction.Back => "back",
        Direction.Left => "left",
        _ => "right"
    };

    public static IReadOnlyList<Direction> ParseSides(string[] names, IReadOnlyList<Direction> excluded, Direction[] fallback)
    {
        var sides = new List<Direction>();
        if (names != null)
        {
            foreach (var name in names)
            {
                if (Enum.TryParse(name, true, out Direction side) && !Contains(excluded, side) && !sides.Contains(side))
                    sides.Add(side);
            }
        }

        if (sides.Count == 0)
        {
            foreach (var side in fallback)
            {
                if (!Contains(excluded, side)) sides.Add(side);
            }
        }
        return sides;
    }

    private static bool Contains(IReadOnlyList<Direction> list, Direction side)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == side) return true;
        }
        return false;
    }
}
