using OpenTK.Mathematics;

namespace Spacebox.Game.Radar;

public enum RadarContactKind : byte
{
    Ship,
    Player,
    Mob
}

public struct RadarContact
{
    public RadarContactKind Kind;
    public Vector2 Direction;
    public float Distance;
    public float Alpha;
}
