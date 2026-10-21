using Engine;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public sealed class PreparedEntity
{
    public Asteroid Asteroid { get; init; }
    public Dictionary<Vector3SByte, Block[,,]> Blocks { get; init; }
    public List<Vector3SByte> CompleteChunks { get; init; }
    public MultiblockRepair.Result Repair { get; init; }
}
