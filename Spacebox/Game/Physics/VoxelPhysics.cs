using OpenTK.Mathematics;
using Engine;
using Engine.Physics;
using Spacebox.Game.Generation;
using System.Runtime.CompilerServices;
using Spacebox.Game.Generation.Blocks;


namespace Spacebox.Game.Physics
{
    public class VoxelPhysics
    {
        public static bool IsColliding(BoundingVolume volume, Chunk chunk, Vector3 Position, out CollideInfo collideInfo)
        {
            return IsColliding(volume, chunk.Blocks, Position, out collideInfo);
        }
        public static bool IsColliding(BoundingVolume volume, Block[,,] Blocks, Vector3 ChunkWorldPosition, out CollideInfo collideInfo)
        {
            collideInfo = new CollideInfo();
            if (!(volume is BoundingSphere sphere)) return false;

            Vector3 sphereMin = sphere.Center - new Vector3(sphere.Radius);
            Vector3 sphereMax = sphere.Center + new Vector3(sphere.Radius);
            Vector3 localMin = sphereMin - ChunkWorldPosition;
            Vector3 localMax = sphereMax - ChunkWorldPosition;

            int minX = Math.Max((int)Math.Floor(localMin.X), 0);
            int minY = Math.Max((int)Math.Floor(localMin.Y), 0);
            int minZ = Math.Max((int)Math.Floor(localMin.Z), 0);
            int maxX = Math.Min((int)Math.Ceiling(localMax.X), Chunk.Size - 1);
            int maxY = Math.Min((int)Math.Ceiling(localMax.Y), Chunk.Size - 1);
            int maxZ = Math.Min((int)Math.Ceiling(localMax.Z), Chunk.Size - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Block block = Blocks[x, y, z];
                        collideInfo.block = block;
                        if (!block.IsAir)
                        {
                            Vector3 blockMin = ChunkWorldPosition + new Vector3(x, y, z);
                            BoundingBox blockBox = new BoundingBox(blockMin + new Vector3(0.5f), Vector3.One);
                            if (sphere.Intersects(blockBox))
                            {
                                collideInfo.blockPositionIndex = new Vector3Byte(x, y, z);
                                blockMin = blockBox.Center - blockBox.Size * 0.5f;
                                Vector3 blockMax = blockBox.Center + blockBox.Size * 0.5f;
                                Vector3 clamped = new Vector3(
                                    Math.Clamp(sphere.Center.X, blockMin.X, blockMax.X),
                                    Math.Clamp(sphere.Center.Y, blockMin.Y, blockMax.Y),
                                    Math.Clamp(sphere.Center.Z, blockMin.Z, blockMax.Z)
                                );
                                Vector3 normal = (sphere.Center - clamped).Normalized();
                                collideInfo.normal = (Vector3SByte)normal;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        public static bool RaycastChunks(SpaceEntity entity, Ray ray, out List<ChunkHitInfo> chunkHits)
        {
            chunkHits = new List<ChunkHitInfo>();

            float startDistance = 0;

            if (entity.BoundingBox.Contains(ray.Origin))
            {

            }
            else
            {
                if (!ray.Intersects(entity.BoundingBox, out startDistance))
                {
                    return false;
                }
            }

            if (!entity.BoundingBox.Contains(ray.Origin))
            {

                ray.Origin = ray.GetPoint(startDistance);
            }

            HashSet<Chunk> potentialChunks = new HashSet<Chunk>();

            potentialChunks = entity.Chunks.ToHashSet();

            bool checkOrig = true;

            foreach (var chunk in potentialChunks)
            {

                if (checkOrig && chunk.BoundingBox.Contains(ray.Origin))
                {
                    var ch = new ChunkHitInfo
                    {
                        Chunk = chunk,
                        Distance = 0,
                        HitPosition = ray.Origin
                    };
                    chunkHits.Add(ch);
                    checkOrig = false;
                    continue;
                }

                if (ray.Intersects(chunk.BoundingBox, out float dis))
                {

                    if (dis > ray.Length)
                    {

                        continue;
                    }

                    Vector3 hitPos = ray.GetPoint(dis + 0.01f);

                    chunkHits.Add(new ChunkHitInfo
                    {
                        Chunk = chunk,
                        Distance = dis,
                        HitPosition = hitPos
                    });
                }
            }

            if (chunkHits.Count > 1)
            {
                chunkHits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            }

            return chunkHits.Count > 0;
        }



        public static bool RaycastChunk(Ray ray, Chunk chunk, out HitInfo hitInfo)
        {
            return Raycast(ray, chunk.PositionWorld, ref chunk, out hitInfo);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3SByte GetNearestFaceNormal(Vector3 point, Vector3 cubeMin, Vector3 cubeMax)
        {
            float dx1 = point.X - cubeMin.X, dx2 = cubeMax.X - point.X;
            float dy1 = point.Y - cubeMin.Y, dy2 = cubeMax.Y - point.Y;
            float dz1 = point.Z - cubeMin.Z, dz2 = cubeMax.Z - point.Z;
            float min = dx1;
            Vector3SByte normal = new Vector3SByte(-1, 0, 0);
            if (dx2 < min) { min = dx2; normal = new Vector3SByte(1, 0, 0); }
            if (dy1 < min) { min = dy1; normal = new Vector3SByte(0, -1, 0); }
            if (dy2 < min) { min = dy2; normal = new Vector3SByte(0, 1, 0); }
            if (dz1 < min) { min = dz1; normal = new Vector3SByte(0, 0, -1); }
            if (dz2 < min) { normal = new Vector3SByte(0, 0, 1); }
            return normal;
        }
        private static bool Raycast(Ray ray, Vector3 chunkPosWorld, ref Chunk chunk, out HitInfo hitInfo)
        {
            Block[,,] blocks = chunk.Blocks;

            hitInfo.position = Vector3.Zero;
            hitInfo.blockPositionIndex = new Vector3Byte(0, 0, 0); // or -1 ????
            hitInfo.blockPositionEntity = new Vector3i(0) ;    
            hitInfo.normal = Vector3SByte.Zero;
            hitInfo.chunk = chunk;
            hitInfo.block = null;

            // if (!_isLoadedOrGenerated) return false;

            Vector3 rayOrigin = ray.Origin - chunkPosWorld;
            Vector3 rayDirection = ray.Direction;

            int x = (int)MathF.Floor(rayOrigin.X);
            int y = (int)MathF.Floor(rayOrigin.Y);
            int z = (int)MathF.Floor(rayOrigin.Z);

            float deltaDistX = rayDirection.X == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.X);
            float deltaDistY = rayDirection.Y == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Y);
            float deltaDistZ = rayDirection.Z == 0 ? float.MaxValue : MathF.Abs(1 / rayDirection.Z);

            int stepX = rayDirection.X < 0 ? -1 : 1;
            int stepY = rayDirection.Y < 0 ? -1 : 1;
            int stepZ = rayDirection.Z < 0 ? -1 : 1;

            float sideDistX = rayDirection.X == 0 ? float.MaxValue : (rayDirection.X < 0 ? (rayOrigin.X - x) : (x + 1.0f - rayOrigin.X)) * deltaDistX;
            float sideDistY = rayDirection.Y == 0 ? float.MaxValue : (rayDirection.Y < 0 ? (rayOrigin.Y - y) : (y + 1.0f - rayOrigin.Y)) * deltaDistY;
            float sideDistZ = rayDirection.Z == 0 ? float.MaxValue : (rayDirection.Z < 0 ? (rayOrigin.Z - z) : (z + 1.0f - rayOrigin.Z)) * deltaDistZ;

            float distanceTraveled = 0f;
            int side = -1;

            while (distanceTraveled < ray.Length)
            {
                if (IsInRange(x, y, z))
                {
                    Block block = blocks[x, y, z];
                    if (!block.IsAir)
                    {
                        hitInfo.blockPositionIndex = new Vector3Byte(x, y, z);
                        hitInfo.position = ray.Origin + ray.Direction * distanceTraveled;

                        switch (side)
                        {
                            case 0:
                                hitInfo.normal = new Vector3SByte(-stepX, 0, 0);
                                break;
                            case 1:
                                hitInfo.normal = new Vector3SByte(0, -stepY, 0);
                                break;
                            case 2:
                                hitInfo.normal = new Vector3SByte(0, 0, -stepZ);
                                break;
                            default:

                                hitInfo.normal = GetNearestFaceNormal(rayOrigin, Vector3.Zero, new Vector3(Chunk.Size, Chunk.Size, Chunk.Size));

                                break;
                        }

                        hitInfo.blockPositionEntity = new Vector3i(
                            chunk.PositionIndex.X * Chunk.Size + hitInfo.blockPositionIndex.X,
                             chunk.PositionIndex.Y * Chunk.Size + hitInfo.blockPositionIndex.Y,
                              chunk.PositionIndex.Z * Chunk.Size + hitInfo.blockPositionIndex.Z);

                        hitInfo.block = block;
                        return true;
                    }
                }
                else
                {
                    return false;
                }

                if (sideDistX < sideDistY)
                {
                    if (sideDistX < sideDistZ)
                    {
                        x += stepX;
                        distanceTraveled = sideDistX;
                        sideDistX += deltaDistX;
                        side = 0;
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2;
                    }
                }
                else
                {
                    if (sideDistY < sideDistZ)
                    {
                        y += stepY;
                        distanceTraveled = sideDistY;
                        sideDistY += deltaDistY;
                        side = 1;
                    }
                    else
                    {
                        z += stepZ;
                        distanceTraveled = sideDistZ;
                        sideDistZ += deltaDistZ;
                        side = 2;
                    }
                }
            }

            return false;
        }

        private static float CalcSideDist(
            float dirComponent,
            float originComponent,
            int index,
            float deltaDist,
            int step
        )
        {
            if (dirComponent == 0)
                return float.MaxValue;

            if (dirComponent < 0)
                return (originComponent - index) * deltaDist;
            else
                return (index + 1.0f - originComponent) * deltaDist;
        }


        private static bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Chunk.Size && y >= 0 && y < Chunk.Size && z >= 0 && z < Chunk.Size;
        }
    }
}
