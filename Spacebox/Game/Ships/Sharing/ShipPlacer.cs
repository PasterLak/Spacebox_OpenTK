using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Protection;
using Spacebox.Game.Resource;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipPlacer
{
    public static Vector3 CenterOf(CompoundTag shipEntity)
    {
        var stats = ShipTagReader.Measure(shipEntity);
        return ((Vector3)stats.Min + (Vector3)stats.Max) * 0.5f;
    }

    public static Vector3 OriginFor(Vector3 center, Vector3 localCenter, Quaternion rotation) => center - Vector3.Transform(localCenter, rotation);

    public static bool TryPlace(CompoundTag shipEntity, Vector3 center, Quaternion rotation, string owner, out string message, out SpaceEntity placed)
    {
        placed = null;
        message = ShipImportTag.Validate(shipEntity);
        if (message != null) return false;
        if (!NetValidationFinite(center) || !NetValidationFinite(rotation))
        {
            message = "Invalid position";
            return false;
        }

        rotation = rotation.Normalized();
        var origin = OriginFor(center, CenterOf(shipEntity), rotation);
        var sector = World.SectorAt(origin);
        if (sector == null)
        {
            message = "That place is not loaded";
            return false;
        }

        long id = sector.GenerateDynamicEntityId();
        var local = origin - SpaceMath.Sector.GetSectorPosition(sector.PositionIndex);
        var missing = new SortedDictionary<string, int>();
        string air = GameAssets.GetBlockDataById(0)?.Id_string ?? "default:void";
        var tag = ShipImportTag.Build(shipEntity, id, sector.PositionIndex, local, rotation, fullId => GameAssets.BlocksStr.ContainsKey(fullId), air, missing);

        var entity = NBTHelper.TagToSpaceEntity(tag, sector, null, NBTHelper.PrepareEntity(tag, sector));
        if (entity == null)
        {
            message = "The ship could not be built";
            return false;
        }

        sector.AddCreatedEntity(entity);
        AssignBeacons(entity, owner);
        placed = entity;

        message = missing.Count == 0
            ? "Ship placed"
            : $"Ship placed without blocks this world does not have: {string.Join(", ", missing.Keys)}";
        return true;
    }

    private static void AssignBeacons(SpaceEntity entity, string owner)
    {
        foreach (var chunk in entity.Chunks)
        {
            var origin = new Vector3i(chunk.PositionIndex.X, chunk.PositionIndex.Y, chunk.PositionIndex.Z) * Chunk.Size;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                if (chunk.Blocks[x, y, z] is ClaimBeaconBlock) ClaimPlacement.AssignOwner(entity, origin + new Vector3i(x, y, z), owner);
            }
        }
    }

    private static bool NetValidationFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private static bool NetValidationFinite(Quaternion q) => float.IsFinite(q.X) && float.IsFinite(q.Y) && float.IsFinite(q.Z) && float.IsFinite(q.W) && q.LengthSquared > 1e-6f;
}
