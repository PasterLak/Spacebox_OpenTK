using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Resource;

public interface IReservedAreaBlockData
{
    bool AreaMustBeInsideEntity { get; }
    void GetReservedArea(Direction direction, Rotation rotation, List<Vector3i> offsets);
}
