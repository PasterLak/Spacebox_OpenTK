using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public readonly struct MobTarget
{
    public readonly int Peer;
    public readonly Vector3 Position;
    public readonly bool IsVulnerable;
    public readonly bool HasLight;

    public MobTarget(int peer, Vector3 position, bool isVulnerable, bool hasLight = true)
    {
        Peer = peer;
        Position = position;
        IsVulnerable = isVulnerable;
        HasLight = hasLight;
    }
}
