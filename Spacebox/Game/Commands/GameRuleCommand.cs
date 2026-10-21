using Engine;
using Engine.Commands;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class GameRuleCommand : CommandBase
{
    public override string Name => "gamerule";
    public override string Description => "Server rules. Usage: gamerule [name] [on|off] (pvp, explosions, explosion_blocks, explosion_players, explosion_claims, friendly_mobs)";

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (args.Length == 0)
        {
            foreach (var rule in ServerRulesService.Rules)
                Debug.Log($"{rule.Name}: {OnOff(rule.Get())} ({rule.Label})");
            return;
        }

        if (!ServerRulesService.TryFind(args[0], out var found))
        {
            Debug.Error($"Unknown rule <{args[0]}>, see gamerule");
            return;
        }

        if (args.Length == 1)
        {
            Debug.Log($"{found.Name}: {OnOff(found.Get())}");
            return;
        }

        if (!TryParseSwitch(args[1], out bool value))
        {
            Debug.Error("Use on or off");
            return;
        }

        ServerRulesService.Set(found, value);
        Debug.Success($"{found.Label}: {OnOff(value)}");
    }

    private static string OnOff(bool value) => value ? "on" : "off";

    public static bool TryParseSwitch(string text, out bool value)
    {
        value = false;
        switch (text?.Trim().ToLowerInvariant())
        {
            case "on":
            case "true":
            case "1":
                value = true;
                return true;
            case "off":
            case "false":
            case "0":
                return true;
            default:
                return false;
        }
    }
}
