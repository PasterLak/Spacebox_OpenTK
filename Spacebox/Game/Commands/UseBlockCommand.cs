using Engine;
using Engine.Commands;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Interactions;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

internal sealed class UseBlockCommand : CommandBase
{
    private readonly LocalAstronaut _player;

    public UseBlockCommand(LocalAstronaut player)
    {
        _player = player;
    }

    public override string Name => "use";
    public override string Description => "Uses the block under the crosshair, like pressing the use key";
    public override bool AvailableInTerminal => false;

    public override void Execute(string[] args)
    {
        if (ChatCommands.RefusesRemoteSender(Name)) return;
        if (_player == null || !BlockUseTarget.TryFind(_player, out var block, out var hit))
        {
            Debug.Warning("No usable block under the crosshair");
            return;
        }

        InteractiveBlock.Use(block, _player, ref hit);
        Debug.Log($"Used {GameAssets.GetBlockDataById(block.Id)?.Name ?? "block"}");
    }
}
