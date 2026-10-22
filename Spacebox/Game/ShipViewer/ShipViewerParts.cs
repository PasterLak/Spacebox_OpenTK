using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;

namespace Spacebox.Game.ShipViewer;

public readonly record struct ViewerThruster(Vector3 Exhaust, Vector3 Direction, float Nozzle);

public readonly record struct ViewerCannon(Vector3 Muzzle, Vector3 Direction, CannonBlockData Data);

public sealed class ShipViewerParts
{
    public List<ViewerThruster> Thrusters { get; } = new List<ViewerThruster>();
    public List<ViewerCannon> Cannons { get; } = new List<ViewerCannon>();
    public ShipViewOrientation Orientation { get; } = new ShipViewOrientation();
    public Vector3 Min { get; private set; } = new Vector3(float.MaxValue);
    public Vector3 Max { get; private set; } = new Vector3(float.MinValue);
    public Vector3 Extent => Max.X < Min.X ? Vector3.One : Max - Min;

    public static ShipViewerParts Scan(IReadOnlyDictionary<Vector3SByte, Block[,,]> chunks)
    {
        var parts = new ShipViewerParts();
        foreach (var pair in chunks)
        {
            var origin = new Vector3i(pair.Key.X, pair.Key.Y, pair.Key.Z) * Chunk.Size;
            var blocks = pair.Value;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                var block = blocks[x, y, z];
                if (block == null || block.IsAir) continue;
                parts.Add(block, origin + new Vector3i(x, y, z));
            }
        }

        return parts;
    }

    private void Add(Block block, Vector3i cell)
    {
        var position = new Vector3(cell.X, cell.Y, cell.Z);
        Min = Vector3.ComponentMin(Min, position);
        Max = Vector3.ComponentMax(Max, position + Vector3.One);
        if (block.HasMultiblockOffset) return;

        switch (GameAssets.GetBlockDataById(block.Id))
        {
            case ShipConsoleBlockData console:
                Orientation.AddConsole(ShipPartFrame.Compute(block, cell).Up, console.Flight);
                break;
            case ThrusterBlockData thruster:
                var thrusterFrame = ShipPartFrame.Compute(block, cell);
                Orientation.AddThruster(-thrusterFrame.Front, thruster.Force);
                Thrusters.Add(new ViewerThruster(thrusterFrame.FrontFacePoint, thrusterFrame.Front, ThrusterExhaustEmitter.NozzleSize(thrusterFrame)));
                break;
            case CannonBlockData cannon:
                var cannonFrame = ShipPartFrame.Compute(block, cell);
                Orientation.AddCannon(cannonFrame.Front);
                Cannons.Add(new ViewerCannon(CannonBlock.GetMuzzle(cannonFrame, cannonFrame.Front, cannon), cannonFrame.Front, cannon));
                break;
        }
    }
}
