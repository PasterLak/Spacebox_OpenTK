using System.Text.Json;

namespace Spacebox.Game.Protection;

public sealed class ClaimSettings
{
    public const int MinRadius = 2;
    public const int MaxRadius = 64;
    public const int MinStoredMinutes = 60;
    public const int MaxMembersLimit = 64;
    public const int MaxNameLength = 32;

    public bool Enabled { get; set; } = true;
    public int Radius { get; set; } = 12;
    public int MaxStoredMinutes { get; set; } = 30 * 24 * 60;
    public int WarningMinutes { get; set; } = 60;
    public int ExtraClaimCostPercent { get; set; } = 50;
    public int MaxClaimsPerPlayer { get; set; } = 0;
    public int MaxMembers { get; set; } = 16;
    public int MinDistanceFromSpawn { get; set; } = 0;

    public Dictionary<string, int> Payments { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["aluminium_ingot"] = 30,
        ["iron_ingot"] = 30,
        ["cobalt_ingot"] = 60,
        ["titanium_ingot"] = 10 * 60,
        ["thulium_ingot"] = 12 * 60,
        ["uranium_ingot"] = 24 * 60,
        ["obscurium_ingot"] = 7 * 24 * 60
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public int MaxStoredSeconds => MaxStoredMinutes * 60;

    [System.Text.Json.Serialization.JsonIgnore]
    public int WarningSeconds => WarningMinutes * 60;

    private static ClaimSettings _override;

    public static ClaimSettings Current => _override ?? Spacebox.Network.Dedicated.DedicatedServer.Config?.Claims ?? Defaults;

    public static ClaimSettings Defaults { get; } = new ClaimSettings();

    public static void SetOverride(ClaimSettings settings) => _override = settings;

    public static void ClearOverride() => _override = null;

    public bool Validate()
    {
        var d = Defaults;
        bool changed = false;
        if (Radius < MinRadius || Radius > MaxRadius) { Radius = d.Radius; changed = true; }
        if (MaxStoredMinutes < MinStoredMinutes) { MaxStoredMinutes = d.MaxStoredMinutes; changed = true; }
        if (WarningMinutes < 0) { WarningMinutes = d.WarningMinutes; changed = true; }
        if (ExtraClaimCostPercent < 0 || ExtraClaimCostPercent > 1000) { ExtraClaimCostPercent = d.ExtraClaimCostPercent; changed = true; }
        if (MaxClaimsPerPlayer < 0) { MaxClaimsPerPlayer = 0; changed = true; }
        if (MaxMembers < 0 || MaxMembers > MaxMembersLimit) { MaxMembers = d.MaxMembers; changed = true; }
        if (MinDistanceFromSpawn < 0) { MinDistanceFromSpawn = 0; changed = true; }

        if (Payments == null)
        {
            Payments = new Dictionary<string, int>(d.Payments, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        var clean = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Payments)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0) { changed = true; continue; }
            clean[pair.Key.Trim()] = pair.Value;
        }
        if (clean.Count != Payments.Count || !ReferenceEquals(Payments.Comparer, StringComparer.OrdinalIgnoreCase)) Payments = clean;

        return changed;
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static ClaimSettings FromJson(string json)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<ClaimSettings>(json ?? "") ?? new ClaimSettings();
            settings.Validate();
            return settings;
        }
        catch (JsonException)
        {
            return new ClaimSettings();
        }
    }
}
