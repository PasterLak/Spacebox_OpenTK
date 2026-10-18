using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public readonly struct MobTarget
{
    public readonly int Peer;
    public readonly Vector3 Position;
    public readonly bool IsVulnerable;

    public MobTarget(int peer, Vector3 position, bool isVulnerable)
    {
        Peer = peer;
        Position = position;
        IsVulnerable = isVulnerable;
    }
}
