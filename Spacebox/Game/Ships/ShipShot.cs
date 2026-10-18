using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public struct ShipShot
{
    public short CannonBlockId;
    public long EntityId;
    public int ShooterPeer;
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 CarrierVelocity;
}
