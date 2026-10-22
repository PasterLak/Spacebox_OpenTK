using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Generation.Tools;

public static class EntityTagLocation
{
    public static void Write(CompoundTag root, Vector3i sector, Vector3 local)
    {
        Set(root, new IntTag(NBTKey.ENTITY.sector_x, sector.X));
        Set(root, new IntTag(NBTKey.ENTITY.sector_y, sector.Y));
        Set(root, new IntTag(NBTKey.ENTITY.sector_z, sector.Z));
        Set(root, new FloatTag(NBTKey.ENTITY.local_x, local.X));
        Set(root, new FloatTag(NBTKey.ENTITY.local_y, local.Y));
        Set(root, new FloatTag(NBTKey.ENTITY.local_z, local.Z));
    }

    public static bool TryReadSector(CompoundTag tag, out Vector3i sector)
    {
        sector = default;
        if (tag == null
            || !tag.TryGetValue<IntTag>(NBTKey.ENTITY.sector_x, out var x)
            || !tag.TryGetValue<IntTag>(NBTKey.ENTITY.sector_y, out var y)
            || !tag.TryGetValue<IntTag>(NBTKey.ENTITY.sector_z, out var z))
            return false;

        sector = new Vector3i(x.Value, y.Value, z.Value);
        return true;
    }

    public static bool TryReadLocal(CompoundTag tag, out Vector3 local)
    {
        local = default;
        if (tag == null
            || !tag.TryGetValue<FloatTag>(NBTKey.ENTITY.local_x, out var x)
            || !tag.TryGetValue<FloatTag>(NBTKey.ENTITY.local_y, out var y)
            || !tag.TryGetValue<FloatTag>(NBTKey.ENTITY.local_z, out var z))
            return false;

        local = new Vector3(x.Value, y.Value, z.Value);
        return true;
    }

    public static Vector3 ReadWorld(CompoundTag tag, Vector3 fallbackSectorOrigin)
    {
        TryReadLocal(tag, out var local);
        var origin = TryReadSector(tag, out var sector) ? SpaceMath.Sector.GetSectorPosition(sector) : fallbackSectorOrigin;
        return origin + local;
    }

    private static void Set(CompoundTag root, Tag tag)
    {
        root.Remove(tag.Name);
        root.Add(tag);
    }
}
