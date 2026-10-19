using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Physics;

public struct SphereContact
{
    public Vector3 Normal;
    public float Depth;
    public CollideInfo CollideInfo;
}

public static class VoxelSphereContacts
{
    private const float EntityQueryMargin = 1f;

    public static bool TryFindDeepest(Sector sector, Vector3 centerWorld, float radius, out SphereContact contact)
    {
        contact = default;
        float margin = radius * 2f + EntityQueryMargin;
        float marginSquared = margin * margin;

        bool found = FindIn(sector.Entities, centerWorld, radius, marginSquared, ref contact);
        found |= FindIn(sector.Visitors, centerWorld, radius, marginSquared, ref contact);
        return found;
    }

    private static bool FindIn(IReadOnlyList<SpaceEntity> entities, Vector3 centerWorld, float radius, float marginSquared, ref SphereContact contact)
    {
        bool found = false;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Chunks.Count == 0) continue;
            if (SpaceMath.World.DistanceToBox(entity.ToEntitySpace(centerWorld), entity.GeometryBoundingBox) > marginSquared) continue;
            found |= FindIn(entity, centerWorld, radius, ref contact);
        }
        return found;
    }

    private static bool FindIn(SpaceEntity entity, Vector3 centerWorld, float radius, ref SphereContact contact)
    {
        var center = entity.WorldToEntityLocal(centerWorld);
        int minX = (int)MathF.Floor(center.X - radius), maxX = (int)MathF.Floor(center.X + radius);
        int minY = (int)MathF.Floor(center.Y - radius), maxY = (int)MathF.Floor(center.Y + radius);
        int minZ = (int)MathF.Floor(center.Z - radius), maxZ = (int)MathF.Floor(center.Z + radius);
        bool found = false;

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            var cell = new Vector3i(x, y, z);
            if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) continue;

            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (!BlockCollision.IsSolid(block)) continue;

            BlockCollision.GetCellBounds(entity, cell, block, out var min, out var max);
            var origin = new Vector3(x, y, z);
            if (!SphereBoxMath.TryGetContact(center, radius, min + origin, max + origin, out var normal, out float depth)) continue;
            if (depth <= contact.Depth) continue;

            contact.Normal = entity.DirectionToWorldSpace(normal);
            contact.Depth = depth;
            contact.CollideInfo = new CollideInfo
            {
                block = block,
                chunk = chunk,
                blockPositionIndex = index,
                normal = (Vector3SByte)contact.Normal
            };
            found = true;
        }

        if (entity.Doors.TryGetMovingDoorContact(center, radius, out var doorNormal, out float doorDepth) && doorDepth > contact.Depth)
        {
            contact.Normal = entity.DirectionToWorldSpace(doorNormal);
            contact.Depth = doorDepth;
            contact.CollideInfo = default;
            found = true;
        }

        return found;
    }
}
