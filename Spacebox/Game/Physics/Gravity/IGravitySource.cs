using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics.Gravity;

public interface IGravitySource
{
    bool IsEmitting { get; }
    SpaceEntity Entity { get; }
    Vector3 WorldCenter { get; }
    Vector3 WorldDirection { get; }
    float Radius { get; }
    float Strength { get; }
    Spacebox.Game.Resource.FallDamageJSON FallDamage { get; }
    bool Affects(Vector3 worldPosition);
}
