using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Protection;

public static class ClaimVisuals
{
    public const float HighlightSeconds = 3f;
    public const float HoldingViewDistance = 64f;
    public const float Thickness = 0.12f;
    public const float EnterCheckInterval = 0.5f;

    private static readonly Color4 OwnColor = new Color4(0.3f, 1f, 0.45f, 0.85f);
    private static readonly Color4 ForeignColor = new Color4(1f, 0.3f, 0.3f, 0.85f);
    private static readonly Color4 InactiveColor = new Color4(0.6f, 0.6f, 0.6f, 0.6f);

    private static ClaimBeaconBlock _highlighted;
    private static float _highlightTime;
    private static ClaimBeaconBlock _currentForeign;
    private static float _enterTimer;

    public static void Highlight(ClaimBeaconBlock beacon)
    {
        _highlighted = beacon;
        _highlightTime = HighlightSeconds;
    }

    public static void Update(LocalAstronaut player, float deltaTime)
    {
        if (player == null || !ClaimSettings.Current.Enabled) return;

        UpdateEnterHint(player, deltaTime);
        DrawOutlines(player, deltaTime);
    }

    public static void Reset()
    {
        _highlighted = null;
        _highlightTime = 0f;
        _currentForeign = null;
        _enterTimer = 0f;
    }

    public static bool IsHoldingBeacon()
    {
        var slot = PanelUI.CurrentSlot();
        return slot != null && slot.HasItem && slot.Item is BlockItem blockItem && GameAssets.GetBlockDataById(blockItem.BlockId) is ClaimBeaconBlockData;
    }

    private static void DrawOutlines(LocalAstronaut player, float deltaTime)
    {
        var claimBeacon = GameUi.Current?.ClaimBeacon;
        var open = claimBeacon != null && claimBeacon.ShowZone ? claimBeacon.OpenBlock : null;
        if (open != null) DrawZone(open);

        if (_highlightTime > 0f)
        {
            _highlightTime -= deltaTime;
            if (_highlighted?.Entity != null && _highlighted != open) DrawZone(_highlighted);
        }

        if (!IsHoldingBeacon()) return;

        var beacons = ClaimRegistry.Loaded;
        for (int i = 0; i < beacons.Count; i++)
        {
            var beacon = beacons[i];
            if (beacon == open || beacon.Entity == null) continue;
            var center = beacon.Entity.EntityLocalToWorld(new Vector3(beacon.LocalCell.X + 0.5f, beacon.LocalCell.Y + 0.5f, beacon.LocalCell.Z + 0.5f));
            if ((center - player.Position).LengthSquared > HoldingViewDistance * HoldingViewDistance) continue;
            DrawZone(beacon);
        }
    }

    private static void DrawZone(ClaimBeaconBlock beacon)
    {
        var entity = beacon.Entity;
        if (entity == null) return;

        var color = !beacon.IsClaimActive ? InactiveColor : IsLocalAllowed(beacon) ? OwnColor : ForeignColor;

        if (beacon.CoversWholeEntity)
        {
            var box = entity.GeometryBoundingBox;
            WorldLines.DrawBox(box.Min, box.Max, color, Thickness, entity.ToWorldSpace);
            return;
        }

        int radius = ClaimSettings.Current.Radius;
        var cell = beacon.LocalCell;
        var min = new Vector3(cell.X - radius, cell.Y - radius, cell.Z - radius);
        var max = new Vector3(cell.X + radius + 1, cell.Y + radius + 1, cell.Z + radius + 1);
        WorldLines.DrawBox(min, max, color, Thickness, entity.EntityLocalToWorld);
    }

    private static bool IsLocalAllowed(ClaimBeaconBlock beacon) => EntityProtection.LocalCanEdit || beacon.IsAllowed(Spacebox.Network.GameSession.PlayerName);

    private static void UpdateEnterHint(LocalAstronaut player, float deltaTime)
    {
        _enterTimer -= deltaTime;
        if (_enterTimer > 0f) return;
        _enterTimer = EnterCheckInterval;

        var foreign = FindForeignClaimAt(player.Position);
        if (foreign != null && foreign != _currentForeign)
            Chat.Write($"You entered the protected area of {foreign.Owner}", Color4.Yellow);
        _currentForeign = foreign;
    }

    private static ClaimBeaconBlock FindForeignClaimAt(Vector3 position)
    {
        var sector = World.CurrentSector;
        if (sector == null || !sector.TryGetNearestEntity(position, out var entity)) return null;

        var local = entity.WorldToEntityLocal(position);
        var cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
        if (!ClaimRegistry.TryFindActive(entity, cell, out var beacon) || IsLocalAllowed(beacon)) return null;
        return beacon;
    }
}
