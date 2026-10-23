using Engine;
using Engine.Commands;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Respawn;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class SpawnCommand : CommandBase
{
    private readonly LocalAstronaut _player;

    public SpawnCommand(LocalAstronaut player)
    {
        _player = player;
    }

    public override string Name => "spawn";

    public override string Description => "Teleport to the spawn point";

    public override bool AvailableInTerminal => false;

    public override void Execute(string[] args)
    {
        if (ChatCommands.RefusesRemoteSender(Name)) return;
        if (_player == null) return;

        _player.Teleport(SafeSpawnFinder.FindFreePosition(WorldSpawn.For(_player)));
        Debug.Success("Teleported to the spawn point");
    }
}
