using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.Game.Protection;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipPlacementCheck
{
    public const string OverlapsBlocks = "The ship would overlap other blocks";
    public const string OverlapsPlayer = "The ship would overlap you";
    public const string InClaim = "That place is claimed by another player";
    public const string InProtected = "That place is protected by an admin";

    private const float PlayerMargin = 1f;
    private const float CellMargin = 0.4f;

    private static readonly HashSet<long> _checked = new HashSet<long>();

    public static string Find(ShipPlacementBounds bounds, string player, bool isAdmin, Vector3? playerPosition)
    {
        if (playerPosition is { } position && bounds.Overlaps(position, PlayerMargin)) return OverlapsPlayer;

        var world = World.Instance;
        if (world == null) return null;

        _checked.Clear();
        float claimReach = isAdmin ? 0f : ClaimSettings.Current.Radius;
        foreach (var sector in world.LoadedSectors)
        {
            string reason = CheckAll(sector.Entities, bounds, player, isAdmin, claimReach);
            if (reason == null && sector.Visitors != null) reason = CheckAll(sector.Visitors, bounds, player, isAdmin, claimReach);
            if (reason != null) return reason;
        }

        return null;
    }

    private static string CheckAll(IReadOnlyList<SpaceEntity> entities, ShipPlacementBounds bounds, string player, bool isAdmin, float claimReach)
    {
        if (entities == null) return null;

        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity == null || entity.IsDisposed || entity.Chunks.Count == 0 || !_checked.Add(entity.EntityID)) continue;

            string reason = Check(entity, bounds, player, isAdmin, claimReach);
            if (reason != null) return reason;
        }

        return null;
    }

    private static string Check(SpaceEntity entity, ShipPlacementBounds bounds, string player, bool isAdmin, float claimReach)
    {
        float reach = bounds.Radius;
        float distanceSquared = DistanceSquaredToBox(entity.GeometryBoundingBox, entity.ToEntitySpace(bounds.Center));
        bool near = distanceSquared <= reach * reach;
        float claimDistance = reach + claimReach;
        if (!near && distanceSquared > claimDistance * claimDistance) return null;

        LocalCellRange(entity, bounds, out var min, out var max);

        if (!isAdmin)
        {
            if (near && EntityProtection.IsProtected(entity)) return InProtected;
            if (ClaimRegistry.TryFindActiveInBox(entity, min, max, player, out _)) return InClaim;
        }

        return near && OverlapsSolid(entity, bounds, min, max) ? OverlapsBlocks : null;
    }

    private static void LocalCellRange(SpaceEntity entity, ShipPlacementBounds bounds, out Vector3i min, out Vector3i max)
    {
        var low = new Vector3(float.MaxValue);
        var high = new Vector3(float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            var local = entity.WorldToEntityLocal(bounds.Corner(i));
            low = Vector3.ComponentMin(low, local);
            high = Vector3.ComponentMax(high, local);
        }

        min = new Vector3i((int)MathF.Floor(low.X), (int)MathF.Floor(low.Y), (int)MathF.Floor(low.Z));
        max = new Vector3i((int)MathF.Floor(high.X), (int)MathF.Floor(high.Y), (int)MathF.Floor(high.Z));
    }

    private static bool OverlapsSolid(SpaceEntity entity, ShipPlacementBounds bounds, Vector3i min, Vector3i max)
    {
        var chunks = entity.Chunks;
        for (int c = 0; c < chunks.Count; c++)
        {
            var chunk = chunks[c];
            var origin = new Vector3i(chunk.PositionIndex.X * Chunk.Size, chunk.PositionIndex.Y * Chunk.Size, chunk.PositionIndex.Z * Chunk.Size);
            var from = Vector3i.ComponentMax(min, origin) - origin;
            var to = Vector3i.ComponentMin(max, origin + new Vector3i(Chunk.Size - 1)) - origin;
            if (from.X > to.X || from.Y > to.Y || from.Z > to.Z) continue;

            for (int x = from.X; x <= to.X; x++)
            for (int y = from.Y; y <= to.Y; y++)
            for (int z = from.Z; z <= to.Z; z++)
            {
                if (!BlockCollision.IsSolid(chunk.Blocks[x, y, z])) continue;

                var center = new Vector3(origin.X + x + 0.5f, origin.Y + y + 0.5f, origin.Z + z + 0.5f);
                if (bounds.Overlaps(entity.EntityLocalToWorld(center), CellMargin)) return true;
            }
        }

        return false;
    }

    private static float DistanceSquaredToBox(Engine.Physics.BoundingBox box, Vector3 point)
    {
        var closest = Vector3.Clamp(point, box.Min, box.Max);
        return (point - closest).LengthSquared;
    }
}
