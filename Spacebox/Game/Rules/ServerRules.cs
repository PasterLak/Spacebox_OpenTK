using System.Text.Json;

namespace Spacebox.Game.Rules;

public sealed class ServerRules
{
    public const int MinDeathDropMinutes = 1;
    public const int MaxDeathDropMinutes = 1440;
    public const int MaxShipSpeedLimit = 1000;

    private static readonly ServerRules SessionRules = new ServerRules();
    private static ServerRules _override;

    public bool Pvp { get; set; } = true;
    public bool Explosions { get; set; } = true;
    public bool ExplosionBlockDamage { get; set; } = true;
    public bool ExplosionPlayerDamage { get; set; } = true;
    public bool ExplosionsRespectClaims { get; set; } = true;
    public bool KeepInventory { get; set; } = true;
    public int DeathDropMinutes { get; set; } = 10;
    public int MaxShipSpeed { get; set; } = 0;

    [System.Text.Json.Serialization.JsonIgnore]
    public float DeathDropSeconds => Math.Clamp(DeathDropMinutes, MinDeathDropMinutes, MaxDeathDropMinutes) * 60f;

    public static ServerRules Current => Spacebox.Network.Dedicated.DedicatedServer.Config?.Gameplay ?? _override ?? SessionRules;

    public static ServerRules Session => SessionRules;

    public static void ResetSession()
    {
        SessionRules.CopyFrom(new ServerRules());
        _override = null;
    }

    public static void SetOverride(ServerRules rules) => _override = rules;

    public bool Validate()
    {
        int clamped = Math.Clamp(DeathDropMinutes, MinDeathDropMinutes, MaxDeathDropMinutes);
        int speed = Math.Clamp(MaxShipSpeed, 0, MaxShipSpeedLimit);
        if (clamped == DeathDropMinutes && speed == MaxShipSpeed) return false;
        DeathDropMinutes = clamped;
        MaxShipSpeed = speed;
        return true;
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static ServerRules FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new ServerRules();
        try
        {
            var rules = JsonSerializer.Deserialize<ServerRules>(json) ?? new ServerRules();
            rules.Validate();
            return rules;
        }
        catch (JsonException)
        {
            return new ServerRules();
        }
    }

    public void CopyFrom(ServerRules other)
    {
        Pvp = other.Pvp;
        Explosions = other.Explosions;
        ExplosionBlockDamage = other.ExplosionBlockDamage;
        ExplosionPlayerDamage = other.ExplosionPlayerDamage;
        ExplosionsRespectClaims = other.ExplosionsRespectClaims;
        KeepInventory = other.KeepInventory;
        DeathDropMinutes = other.DeathDropMinutes;
        MaxShipSpeed = other.MaxShipSpeed;
    }
}
