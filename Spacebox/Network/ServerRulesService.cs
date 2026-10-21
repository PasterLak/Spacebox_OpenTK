using OpenTK.Mathematics;
using Spacebox.Game.Mobs;
using Spacebox.Game.Rules;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network;

public static class ServerRulesService
{
    public static readonly IReadOnlyList<ServerRuleDefinition> Rules = new[]
    {
        new ServerRuleDefinition("pvp", "PvP", () => ServerRules.Current.Pvp, v => ServerRules.Current.Pvp = v),
        new ServerRuleDefinition("explosions", "Explosions", () => ServerRules.Current.Explosions, v => ServerRules.Current.Explosions = v),
        new ServerRuleDefinition("explosion_blocks", "Explosions break blocks", () => ServerRules.Current.ExplosionBlockDamage, v => ServerRules.Current.ExplosionBlockDamage = v),
        new ServerRuleDefinition("explosion_players", "Explosions hurt players", () => ServerRules.Current.ExplosionPlayerDamage, v => ServerRules.Current.ExplosionPlayerDamage = v),
        new ServerRuleDefinition("explosion_claims", "Explosions respect claims", () => ServerRules.Current.ExplosionsRespectClaims, v => ServerRules.Current.ExplosionsRespectClaims = v),
        new ServerRuleDefinition("friendly_mobs", "Friendly mobs", () => MobRules.FriendlyMobs, SetFriendlyMobs)
    };

    public static bool TryFind(string name, out ServerRuleDefinition rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        foreach (var candidate in Rules)
        {
            if (!string.Equals(candidate.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            rule = candidate;
            return true;
        }
        return false;
    }

    public static void Set(ServerRuleDefinition rule, bool value)
    {
        Apply(new Dictionary<string, bool> { [rule.Name] = value });
    }

    public static void Apply(IReadOnlyDictionary<string, bool> values)
    {
        var changed = new List<string>();
        foreach (var rule in Rules)
        {
            if (!values.TryGetValue(rule.Name, out bool value) || rule.Get() == value) continue;
            rule.Set(value);
            changed.Add($"{rule.Label}: {(value ? "on" : "off")}");
        }

        if (changed.Count == 0) return;
        DedicatedServer.Config?.Save();
        ChatService.Broadcast($"[Server] {string.Join(", ", changed)}", Color4.Orange);
    }

    public static Dictionary<string, bool> Snapshot()
    {
        var values = new Dictionary<string, bool>();
        foreach (var rule in Rules) values[rule.Name] = rule.Get();
        return values;
    }

    private static void SetFriendlyMobs(bool value)
    {
        MobRules.FriendlyMobs = value;
        if (DedicatedServer.Config != null) DedicatedServer.Config.FriendlyMobs = value;
    }
}
