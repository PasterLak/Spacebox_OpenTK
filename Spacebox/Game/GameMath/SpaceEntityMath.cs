using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;

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

    private void CheckAndAddNeighbor(SpaceEntity entity, Vector3SByte idx, int x, int y, int z, HashSet<Chunk> modified)
    {
        if (x == 0) AddNeighbor(entity, idx, -1, 0, 0, modified);
        else if (x == Chunk.Size - 1) AddNeighbor(entity, idx, 1, 0, 0, modified);

        if (y == 0) AddNeighbor(entity, idx, 0, -1, 0, modified);
        else if (y == Chunk.Size - 1) AddNeighbor(entity, idx, 0, 1, 0, modified);

        if (z == 0) AddNeighbor(entity, idx, 0, 0, -1, modified);
        else if (z == Chunk.Size - 1) AddNeighbor(entity, idx, 0, 0, 1, modified);
    }

    private void AddNeighbor(SpaceEntity entity, Vector3SByte chunkIndex, sbyte ox, sbyte oy, sbyte oz, HashSet<Chunk> modified)
    {
        Vector3SByte neighborIndex = new Vector3SByte((sbyte)(chunkIndex.X + ox), (sbyte)(chunkIndex.Y + oy), (sbyte)(chunkIndex.Z + oz));
        Vector3 neighborLocalPos = ChunkIndexToLocal(neighborIndex) + new Vector3(Chunk.SizeHalf, Chunk.SizeHalf, Chunk.SizeHalf);

        if (entity.Octree.TryFindDataAtPosition(neighborLocalPos, out Chunk neighbor) && neighbor != null)
        {
            modified.Add(neighbor);
        }
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

        int chunkMinX = (int)MathF.Floor((float)minX / Chunk.Size);
        int chunkMaxX = (int)MathF.Floor((float)maxX / Chunk.Size);
        int chunkMinY = (int)MathF.Floor((float)minY / Chunk.Size);
        int chunkMaxY = (int)MathF.Floor((float)maxY / Chunk.Size);
        int chunkMinZ = (int)MathF.Floor((float)minZ / Chunk.Size);
        int chunkMaxZ = (int)MathF.Floor((float)maxZ / Chunk.Size);

        HashSet<Chunk> modified = new HashSet<Chunk>();

        for (int cx = chunkMinX; cx <= chunkMaxX; cx++)
        {
            for (int cy = chunkMinY; cy <= chunkMaxY; cy++)
            {
                for (int cz = chunkMinZ; cz <= chunkMaxZ; cz++)
                {
                    Vector3SByte idx = new Vector3SByte((sbyte)cx, (sbyte)cy, (sbyte)cz);

                    Vector3 chunkCenter = ChunkIndexToLocal(idx) + new Vector3(Chunk.SizeHalf, Chunk.SizeHalf, Chunk.SizeHalf);

                    if (!entity.Octree.TryFindDataAtPosition(chunkCenter, out Chunk chunk) || chunk == null)
                        continue;

                    int startX = Math.Max(0, minX - cx * Chunk.Size);
                    int endX = Math.Min(Chunk.Size - 1, maxX - cx * Chunk.Size);
                    int startY = Math.Max(0, minY - cy * Chunk.Size);
                    int endY = Math.Min(Chunk.Size - 1, maxY - cy * Chunk.Size);
                    int startZ = Math.Max(0, minZ - cz * Chunk.Size);
                    int endZ = Math.Min(Chunk.Size - 1, maxZ - cz * Chunk.Size);

                    bool chunkModified = false;

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
                                    chunkModified = true;

                                    CheckAndAddNeighbor(entity, idx, x, y, z, modified);
                                }
                            }
                        }
                    }

                    if (chunkModified)
                    {
                        chunk.IsModified = true;
                        modified.Add(chunk);
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

        int chunkMinX = (int)MathF.Floor((float)minX / Chunk.Size);
        int chunkMaxX = (int)MathF.Floor((float)maxX / Chunk.Size);
        int chunkMinY = (int)MathF.Floor((float)minY / Chunk.Size);
        int chunkMaxY = (int)MathF.Floor((float)maxY / Chunk.Size);
        int chunkMinZ = (int)MathF.Floor((float)minZ / Chunk.Size);
        int chunkMaxZ = (int)MathF.Floor((float)maxZ / Chunk.Size);

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

                    bool chunkModified = false;

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
                                    chunkModified = true;

                                    CheckAndAddNeighbor(entity, idx, x, y, z, modified);
                                }
                            }
                        }
                    }

                    if (chunkModified)
                    {
                        c.IsModified = true;
                        modified.Add(c);
                    }
                }
            }
        }

        return modified.ToList();
    }
}