using OpenTK.Mathematics;

namespace Spacebox.Game.Commands.Teleport;

public interface ITeleportable
{
    string Name { get; }
    Vector3 Position { get; }
    Vector3 Front { get; }
    void Teleport(Vector3 position);
}
