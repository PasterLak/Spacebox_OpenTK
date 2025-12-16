using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.GameMath;

public class SpaceEntityMath
{
    public Vector3Byte WorldPositionToBlockInChunk(SpaceEntity entity, Vector3 worldPosition)
    {
        Vector3 relativePosition = worldPosition - entity.PositionWorld;

        int localX = (int)MathF.Floor(relativePosition.X) % Chunk.Size;
        int localY = (int)MathF.Floor(relativePosition.Y) % Chunk.Size;
        int localZ = (int)MathF.Floor(relativePosition.Z) % Chunk.Size;

        if (localX < 0) localX += Chunk.Size;
        if (localY < 0) localY += Chunk.Size;
        if (localZ < 0) localZ += Chunk.Size;

        return new Vector3Byte((byte)localX, (byte)localY, (byte)localZ);
    }
    public Vector3 ChunkIndexToLocal(Vector3SByte chunkIndex)
    {
        var localPos = new Vector3(chunkIndex.X * Chunk.Size, chunkIndex.Y * Chunk.Size, chunkIndex.Z * Chunk.Size);

        return localPos;
    }




    public List<Chunk> RemoveBlocksInLocalBox(SpaceEntity entity, BoundingBox localBox)
    {
        var min = localBox.Min;
        var max = localBox.Max;

        int minX = (int)MathF.Floor(min.X);
        int minY = (int)MathF.Floor(min.Y);
        int minZ = (int)MathF.Floor(min.Z);
        int maxX = (int)MathF.Floor(max.X);
        int maxY = (int)MathF.Floor(max.Y);
        int maxZ = (int)MathF.Floor(max.Z);

        int chunkMinX = minX / Chunk.Size;
        int chunkMaxX = maxX / Chunk.Size;
        int chunkMinY = minY / Chunk.Size;
        int chunkMaxY = maxY / Chunk.Size;
        int chunkMinZ = minZ / Chunk.Size;
        int chunkMaxZ = maxZ / Chunk.Size;

        HashSet<Chunk> modified = new HashSet<Chunk>();

        for (int cx = chunkMinX; cx <= chunkMaxX; cx++)
        {
            for (int cy = chunkMinY; cy <= chunkMaxY; cy++)
            {
                for (int cz = chunkMinZ; cz <= chunkMaxZ; cz++)
                {
                    Vector3SByte idx = new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz);

                    var localPos = SpaceMath.Entity.ChunkIndexToLocal(idx);

                    if (!entity.Octree.TryFindDataAtPosition(localPos, out Chunk chunk)) continue;


                    if (chunk == null) continue;

                    int startX = Math.Max(0, minX - cx * Chunk.Size);
                    int endX = Math.Min(Chunk.Size - 1, maxX - cx * Chunk.Size);
                    int startY = Math.Max(0, minY - cy * Chunk.Size);
                    int endY = Math.Min(Chunk.Size - 1, maxY - cy * Chunk.Size);
                    int startZ = Math.Max(0, minZ - cz * Chunk.Size);
                    int endZ = Math.Min(Chunk.Size - 1, maxZ - cz * Chunk.Size);

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                Block b = chunk.Blocks[x, y, z];
                                if (b != null && !b.IsAir)
                                {
                                    chunk.Blocks[x, y, z] = GameAssets.CreateBlockFromId(0);
                                    chunk.IsModified = true;
                                    modified.Add(chunk);
                                }
                            }
                        }
                    }
                }
            }
        }

        return modified.ToList();
    }



    public List<Chunk> FillBlocksInLocalBox(SpaceEntity entity, BoundingBox localBox, short blockId)
    {
        var min = localBox.Min;
        var max = localBox.Max;

        int minX = (int)MathF.Floor(min.X);
        int minY = (int)MathF.Floor(min.Y);
        int minZ = (int)MathF.Floor(min.Z);
        int maxX = (int)MathF.Floor(max.X);
        int maxY = (int)MathF.Floor(max.Y);
        int maxZ = (int)MathF.Floor(max.Z);

        int chunkMinX = minX / Chunk.Size;
        int chunkMaxX = maxX / Chunk.Size;
        int chunkMinY = minY / Chunk.Size;
        int chunkMaxY = maxY / Chunk.Size;
        int chunkMinZ = minZ / Chunk.Size;
        int chunkMaxZ = maxZ / Chunk.Size;

        HashSet<Chunk> modified = new HashSet<Chunk>();

        for (int cx = chunkMinX; cx <= chunkMaxX; cx++)
        {
            for (int cy = chunkMinY; cy <= chunkMaxY; cy++)
            {
                for (int cz = chunkMinZ; cz <= chunkMaxZ; cz++)
                {
                    Vector3SByte idx = new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz);
                    Chunk c = SpaceEntity.GetOrCreateChunk(entity, idx);

                    int startX = Math.Max(0, minX - cx * Chunk.Size);
                    int endX = Math.Min(Chunk.Size - 1, maxX - cx * Chunk.Size);
                    int startY = Math.Max(0, minY - cy * Chunk.Size);
                    int endY = Math.Min(Chunk.Size - 1, maxY - cy * Chunk.Size);
                    int startZ = Math.Max(0, minZ - cz * Chunk.Size);
                    int endZ = Math.Min(Chunk.Size - 1, maxZ - cz * Chunk.Size);

                    for (int x = startX; x <= endX; x++)
                    {
                        for (int y = startY; y <= endY; y++)
                        {
                            for (int z = startZ; z <= endZ; z++)
                            {
                                Block b = c.Blocks[x, y, z];
                                if (b == null || b.IsAir)
                                {
                                    c.Blocks[x, y, z] = GameAssets.CreateBlockFromId(blockId);
                                    c.IsModified = true;
                                    modified.Add(c);
                                }
                            }
                        }
                    }
                }
            }
        }

        return modified.ToList();
    }


}
