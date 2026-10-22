using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Protection;

public static class ClaimRegistry
{
    private static readonly List<ClaimBeaconBlock> _beacons = new List<ClaimBeaconBlock>();
    private static readonly ClaimIndex _index = new ClaimIndex();

    public static IReadOnlyList<ClaimBeaconBlock> Loaded => _beacons;
    public static ClaimIndex Index => _index;

    public static event Action Changed;

    private static bool IsAuthority => !Spacebox.Network.GameSession.IsRemoteClient;

    public static void Register(ClaimBeaconBlock beacon)
    {
        if (beacon == null || _beacons.Contains(beacon)) return;
        _beacons.Add(beacon);
        UpdateIndex(beacon);
        Changed?.Invoke();
    }

    public static void Unregister(ClaimBeaconBlock beacon)
    {
        if (beacon == null) return;
        if (beacon.Entity != null && IsAuthority) _index.Remove(ClaimIndex.KeyFor(beacon.Entity.EntityID, beacon.LocalCell));
        if (_beacons.Remove(beacon)) Changed?.Invoke();
    }

    public static void UnregisterEntity(SpaceEntity entity)
    {
        bool removed = false;
        for (int i = _beacons.Count - 1; i >= 0; i--)
        {
            if (_beacons[i].Entity != entity) continue;
            _beacons.RemoveAt(i);
            removed = true;
        }
        if (removed) Changed?.Invoke();
    }

    public static void ForgetEntity(long entityId)
    {
        if (IsAuthority && _index.RemoveEntity(entityId)) Changed?.Invoke();
    }

    public static void NotifyStateChanged(ClaimBeaconBlock beacon)
    {
        UpdateIndex(beacon);
        Changed?.Invoke();
    }

    public static bool TryFindActive(SpaceEntity entity, Vector3i cell, out ClaimBeaconBlock beacon)
    {
        beacon = null;
        if (entity == null) return false;

        for (int i = 0; i < _beacons.Count; i++)
        {
            var candidate = _beacons[i];
            if (candidate.Entity != entity || !candidate.IsClaimActive || !candidate.Covers(cell)) continue;
            beacon = candidate;
            return true;
        }
        return false;
    }

    public static bool TryFindActiveOnEntity(SpaceEntity entity, out ClaimBeaconBlock beacon)
    {
        beacon = null;
        if (entity == null) return false;

        for (int i = 0; i < _beacons.Count; i++)
        {
            if (_beacons[i].Entity != entity || !_beacons[i].IsClaimActive) continue;
            beacon = _beacons[i];
            return true;
        }
        return false;
    }

    public static bool TryFindActiveInBox(SpaceEntity entity, Vector3i min, Vector3i max, string player, out ClaimBeaconBlock blocking)
    {
        blocking = null;
        if (entity == null) return false;

        int radius = ClaimSettings.Current.Radius;
        for (int i = 0; i < _beacons.Count; i++)
        {
            var candidate = _beacons[i];
            if (candidate.Entity != entity || !candidate.IsClaimActive || candidate.IsAllowed(player)) continue;
            if (!candidate.CoversWholeEntity && !ClaimZone.BoxIntersects(candidate.LocalCell, radius, min, max)) continue;
            blocking = candidate;
            return true;
        }
        return false;
    }

    public static string CheckPlacement(SpaceEntity entity, Vector3i cell, string owner)
    {
        var settings = ClaimSettings.Current;
        if (!settings.Enabled) return "Claims are disabled on this server";
        if (EntityProtection.IsProtected(entity)) return "This place is already protected by an admin";

        int radius = settings.Radius;
        for (int i = 0; i < _beacons.Count; i++)
        {
            var other = _beacons[i];
            if (other.Entity != entity) continue;
            if (entity.IsDynamic()) return "This ship already has a claim beacon";
            if (!other.IsClaimActive || other.IsAllowed(owner)) continue;
            if (ClaimZone.Overlap(other.LocalCell, radius, cell, radius)) return $"Too close to the claim of {other.Owner}";
        }

        return CheckOwnerLimits(owner, entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f)));
    }

    public static string CheckNewEntityPlacement(Vector3 worldPosition, string owner)
    {
        if (!ClaimSettings.Current.Enabled) return "Claims are disabled on this server";
        return CheckOwnerLimits(owner, worldPosition);
    }

    private static string CheckOwnerLimits(string owner, Vector3 worldPosition)
    {
        var settings = ClaimSettings.Current;
        if (settings.MaxClaimsPerPlayer > 0 && _index.CountAll(owner) >= settings.MaxClaimsPerPlayer)
            return $"You already have {settings.MaxClaimsPerPlayer} claim beacon(s)";

        if (settings.MinDistanceFromSpawn > 0 && Player.Respawn.WorldSpawn.TryGet(out var spawn) && (worldPosition - spawn).Length < settings.MinDistanceFromSpawn)
            return $"Claims are not allowed within {settings.MinDistanceFromSpawn} m of the spawn";

        return null;
    }

    public static void Update(float deltaTime)
    {
        if (!IsAuthority || deltaTime <= 0f) return;

        int percent = ClaimSettings.Current.ExtraClaimCostPercent;
        for (int i = _beacons.Count - 1; i >= 0; i--)
        {
            var beacon = _beacons[i];
            float rate = ClaimCost.BurnRate(_index.CountActive(beacon.Owner), percent);
            beacon.Update(deltaTime, rate);
        }
    }

    public static void Load(string worldFolder)
    {
        _beacons.Clear();
        if (IsAuthority) _index.Load(worldFolder);
        else _index.Reset();
    }

    public static void Reset()
    {
        _beacons.Clear();
        _index.Reset();
    }

    private static void UpdateIndex(ClaimBeaconBlock beacon)
    {
        if (!IsAuthority || beacon.Entity == null || beacon.Owner.Length == 0) return;
        _index.Upsert(ClaimIndex.KeyFor(beacon.Entity.EntityID, beacon.LocalCell), beacon.Owner, beacon.IsClaimActive);
    }
}
