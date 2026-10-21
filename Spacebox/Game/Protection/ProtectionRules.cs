using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Protection;

public static class ProtectionRules
{
    public static Vector3i CellOf(Chunk chunk, Vector3Byte index)
    {
        var origin = chunk.PositionIndex * Chunk.Size;
        return new Vector3i(origin.X + index.X, origin.Y + index.Y, origin.Z + index.Z);
    }

    public static Vector3i CellAt(SpaceEntity entity, Vector3 entitySpacePosition)
    {
        var local = entity.WorldPositionToLocal(entitySpacePosition);
        return new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z));
    }

    public static bool CanEdit(string player, bool isAdmin, SpaceEntity entity, Vector3i cell)
    {
        if (entity == null || isAdmin) return true;
        if (EntityProtection.IsProtected(entity)) return false;
        return !ClaimRegistry.TryFindActive(entity, cell, out var claim) || claim.IsAllowed(player);
    }

    public static bool CanEditBox(string player, bool isAdmin, SpaceEntity entity, Vector3i min, Vector3i max)
    {
        if (entity == null || isAdmin) return true;
        if (EntityProtection.IsProtected(entity)) return false;
        return !ClaimRegistry.TryFindActiveInBox(entity, min, max, player, out _);
    }

    public static bool CanUseEntity(string player, bool isAdmin, SpaceEntity entity)
    {
        if (entity == null || isAdmin) return true;
        if (EntityProtection.IsProtected(entity)) return false;
        return !ClaimRegistry.TryFindActiveOnEntity(entity, out var claim) || !claim.CoversWholeEntity || claim.IsAllowed(player);
    }

    public static bool IsDamageProtected(SpaceEntity entity, Vector3i cell)
    {
        return entity != null && (EntityProtection.IsProtected(entity) || ClaimRegistry.TryFindActive(entity, cell, out _));
    }

    public static bool IsExplosionProtected(SpaceEntity entity, Vector3i cell, bool respectClaims)
    {
        return entity != null && (EntityProtection.IsProtected(entity) || (respectClaims && ClaimRegistry.TryFindActive(entity, cell, out _)));
    }

    public static bool IsMovementLocked(SpaceEntity entity) => EntityProtection.IsProtected(entity);

    public static bool IgnoresExternalImpulses(SpaceEntity entity)
    {
        return IsMovementLocked(entity) || (ClaimRegistry.TryFindActiveOnEntity(entity, out var claim) && claim.CoversWholeEntity);
    }
}
