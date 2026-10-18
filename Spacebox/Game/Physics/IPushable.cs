using OpenTK.Mathematics;

namespace Spacebox.Game.Physics;

public interface IPushable
{
    void Push(Vector3 normal, float targetSpeed);
}
