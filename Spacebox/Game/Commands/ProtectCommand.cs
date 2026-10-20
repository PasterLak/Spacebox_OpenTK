using Engine;
using Engine.Commands;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Protection;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class ProtectCommand : CommandBase
{
    public const float MaxDistance = 64f;

    private readonly LocalAstronaut _player;
    private readonly bool _protect;

    public ProtectCommand(LocalAstronaut player, bool protect)
    {
        _player = player;
        _protect = protect;
    }

    public override string Name => _protect ? "protect" : "unprotect";

    public override string Description => _protect
        ? $"Protect the nearest asteroid or ship (up to {MaxDistance:0} m): only admins can build, break or move it"
        : "Remove the protection from the nearest asteroid or ship";

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (!CommandIssuer.TryGetPosition(_player, out var position))
        {
            Debug.Error($"{Name} works only from the game chat");
            return;
        }

        if (!ProtectionNetwork.TryFindNear(position, MaxDistance, out var entity, out _))
        {
            Debug.Warning($"No asteroid or ship within {MaxDistance:0} m");
            return;
        }

        string label = Describe(entity);
        if (!EntityProtection.Set(entity.EntityID, _protect))
        {
            Debug.Warning(_protect ? $"{label} is already protected" : $"{label} is not protected");
            return;
        }

        ProtectionNetwork.BroadcastState();
        Debug.Success(_protect ? $"{label} is now protected" : $"{label} is no longer protected");
    }

    public static string Describe(SpaceEntity entity) => $"{(entity.IsProcedural() ? "Asteroid" : "Ship")} {entity.EntityID}";
}
