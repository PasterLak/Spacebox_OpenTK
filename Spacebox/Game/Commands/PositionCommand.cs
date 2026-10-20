using Engine;
using Engine.Commands;
using Spacebox.Game.Player;

namespace Spacebox.Game.Commands;

public sealed class PositionCommand : CommandBase
{
    private readonly LocalAstronaut _player;

    public PositionCommand(LocalAstronaut player)
    {
        _player = player;
    }

    public override string Name => "pos";

    public override string Description => "Show your position (for tp)";

    public override bool AvailableInTerminal => false;

    public override void Execute(string[] args)
    {
        if (_player == null) return;

        var position = _player.Position;
        Debug.Log($"Position: {position.X:0.0} {position.Y:0.0} {position.Z:0.0}");
    }
}
