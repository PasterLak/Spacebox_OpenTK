using Engine;
using Engine.Commands;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class GameRuleCommand : CommandBase
{
    public override string Name => "gamerule";
    public override string Description => "Server rules. Usage: gamerule [name] [value] (pvp, explosions, explosion_blocks, explosion_players, explosion_claims, friendly_mobs, keep_inventory, death_drop_minutes)";

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (args.Length == 0)
        {
            foreach (var rule in ServerRulesService.Rules)
                Debug.Log($"{rule.Name}: {rule.Text} ({rule.Label})");
            return;
        }

        if (!ServerRulesService.TryFind(args[0], out var found))
        {
            Debug.Error($"Unknown rule <{args[0]}>, see gamerule");
            return;
        }

        if (args.Length == 1)
        {
            Debug.Log($"{found.Name}: {found.Text}");
            return;
        }

        if (!ServerRulesService.Set(found, args[1]))
        {
            Debug.Error($"Use {found.Usage}");
            return;
        }

        Debug.Success($"{found.Label}: {found.Text}");
    }
}
