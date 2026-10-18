using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource;

public class PipeJSON
{
    public float TransferInterval { get; set; } = 1f;
    public int Power { get; set; } = 5;
    public string[] OutputSides { get; set; } = { "Up", "Left", "Right", "Forward", "Back" };
}

public class PipeBlockData : BlockData
{
    public float TransferInterval { get; }
    public ushort Power { get; }
    public IReadOnlyList<Direction> OutputSides { get; }

    public PipeBlockData(BlockData baseBlock, PipeJSON settings) : base(baseBlock)
    {
        settings ??= new PipeJSON();
        TransferInterval = Math.Clamp(settings.TransferInterval, 0.05f, 60f);
        Power = (ushort)Math.Clamp(settings.Power, 0, 1000);
        OutputSides = ParseSides(settings.OutputSides);
    }

    private static IReadOnlyList<Direction> ParseSides(string[] names)
    {
        var sides = new List<Direction>();
        if (names != null)
        {
            foreach (var name in names)
            {
                if (Enum.TryParse(name, true, out Direction side) && side != Direction.Down && !sides.Contains(side))
                    sides.Add(side);
            }
        }

        if (sides.Count == 0) sides.AddRange(new[] { Direction.Up, Direction.Left, Direction.Right, Direction.Forward, Direction.Back });
        return sides;
    }
}
