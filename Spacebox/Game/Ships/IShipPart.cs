using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Ships;

public interface IShipPart
{
    SpaceEntity Entity { get; }
    Vector3i LocalCell { get; }
    ShipPartFrame Frame { get; }
    bool IsPowered { get; }
}
