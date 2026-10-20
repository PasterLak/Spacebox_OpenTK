using Engine;
using Engine.Commands;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Protection;

namespace Spacebox.Game.Commands;

public sealed class ProtectedListCommand : CommandBase
{
    private readonly LocalAstronaut _player;

    public ProtectedListCommand(LocalAstronaut player)
    {
        _player = player;
    }

    public override string Name => "protected";

    public override string Description => "List the protected asteroids and ships";

    public override void Execute(string[] args)
    {
        if (EntityProtection.Ids.Count == 0)
        {
            Debug.Log("Nothing is protected");
            return;
        }

        bool hasPosition = CommandIssuer.TryGetPosition(_player, out var position);
        var world = World.Instance;
        foreach (var id in EntityProtection.Ids)
        {
            if (world != null && world.TryFindEntity(id, out var entity))
            {
                string distance = hasPosition ? $", {(entity.CenterOfMassWorld - position).Length:0} m away" : "";
                Debug.Log($"{ProtectCommand.Describe(entity)}{distance}");
            }
            else
            {
                Debug.Log($"Entity {id} (not loaded)");
            }
        }
    }
}
