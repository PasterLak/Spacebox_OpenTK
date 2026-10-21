using OpenTK.Mathematics;
using Spacebox.Game.Mobs;
using Spacebox.Game.Rules;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network;

public static class ServerRulesService
{
    public static readonly IReadOnlyList<ServerRuleDefinition> Rules = new ServerRuleDefinition[]
    {
        new SwitchRuleDefinition("pvp", "PvP", () => ServerRules.Current.Pvp, v => ServerRules.Current.Pvp = v),
        new SwitchRuleDefinition("explosions", "Explosions", () => ServerRules.Current.Explosions, v => ServerRules.Current.Explosions = v),
        new SwitchRuleDefinition("explosion_blocks", "Explosions break blocks", () => ServerRules.Current.ExplosionBlockDamage, v => ServerRules.Current.ExplosionBlockDamage = v),
        new SwitchRuleDefinition("explosion_players", "Explosions hurt players", () => ServerRules.Current.ExplosionPlayerDamage, v => ServerRules.Current.ExplosionPlayerDamage = v),
        new SwitchRuleDefinition("explosion_claims", "Explosions respect claims", () => ServerRules.Current.ExplosionsRespectClaims, v => ServerRules.Current.ExplosionsRespectClaims = v),
        new SwitchRuleDefinition("friendly_mobs", "Friendly mobs", () => MobRules.FriendlyMobs, SetFriendlyMobs),
        new SwitchRuleDefinition("keep_inventory", "Keep inventory", () => ServerRules.Current.KeepInventory, v => ServerRules.Current.KeepInventory = v),
        new NumberRuleDefinition("death_drop_minutes", "Death drops last", () => ServerRules.Current.DeathDropMinutes, v => ServerRules.Current.DeathDropMinutes = v,
            ServerRules.MinDeathDropMinutes, ServerRules.MaxDeathDropMinutes, "min")
    };

    public static bool TryFind(string name, out ServerRuleDefinition rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        foreach (var candidate in Rules)
        {
            if (!candidate.Matches(name)) continue;
            rule = candidate;
            return true;
        }
        return false;
    }

    public static bool Set(ServerRuleDefinition rule, string text)
    {
        return Apply(new Dictionary<string, string> { [rule.Name] = text }) >= 0;
    }

    public static int Apply(IReadOnlyDictionary<string, string> values)
    {
        var changed = new List<string>();
        bool invalid = false;
        foreach (var rule in Rules)
        {
            if (!values.TryGetValue(rule.Name, out string text)) continue;

            string before = rule.Text;
            if (!rule.TrySet(text))
            {
                invalid = true;
                continue;
            }
            if (rule.Text != before) changed.Add($"{rule.Label}: {rule.Text}");
        }

        if (changed.Count > 0)
        {
            Save();
            ServerRulesNetwork.Broadcast();
            ChatService.Broadcast($"[Server] {string.Join(", ", changed)}", Color4.Orange);
        }
        return invalid ? -1 : changed.Count;
    }

    public static Dictionary<string, object> Snapshot()
    {
        var values = new Dictionary<string, object>();
        foreach (var rule in Rules) values[rule.Name] = rule.Value;
        return values;
    }

    private static void Save()
    {
        if (DedicatedServer.Config != null) DedicatedServer.Config.Save();
        else if (!GameSession.IsRemoteClient) WorldRulesStore.Save();
    }

    private static void SetFriendlyMobs(bool value)
    {
        MobRules.FriendlyMobs = value;
        if (DedicatedServer.Config != null) DedicatedServer.Config.FriendlyMobs = value;
    }
}
