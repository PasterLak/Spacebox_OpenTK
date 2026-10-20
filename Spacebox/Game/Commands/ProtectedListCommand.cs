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

    public override string Description => "List admin-protected asteroids and ships and the loaded claim beacons";

    public override void Execute(string[] args)
    {
        ListClaims();

        if (EntityProtection.Ids.Count == 0)
        {
            Debug.Log("No admin protection");
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

    private static void ListClaims()
    {
        var claims = ClaimRegistry.Loaded;
        if (claims.Count == 0)
        {
            Debug.Log("No claim beacons loaded");
            return;
        }

        for (int i = 0; i < claims.Count; i++)
        {
            var claim = claims[i];
            string state = claim.IsClaimActive ? $"{ClaimCost.FormatDuration(claim.RemainingSeconds)} left" : "inactive";
            string where = claim.Entity != null ? ProtectCommand.Describe(claim.Entity) : "?";
            Debug.Log($"Claim of {(claim.Owner.Length > 0 ? claim.Owner : "nobody")} on {where}: {state}, {claim.Members.Count} member(s)");
        }
    }
}
