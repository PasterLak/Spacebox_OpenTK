using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Game.Commands.Teleport;

public sealed class LocalTeleportable : ITeleportable
{
    private readonly LocalAstronaut _astronaut;

    public LocalTeleportable(LocalAstronaut astronaut)
    {
        _astronaut = astronaut;
    }

    public string Name => GameSession.PlayerName;
    public Vector3 Position => _astronaut.Position;
    public Vector3 Front => _astronaut.Front;

    public void Teleport(Vector3 position) => _astronaut.Teleport(position);
}
