using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;


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
                        if (!block.IsAir())
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
                    //Debug.Log("no intersaction dis:" + distance);
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
            return Raycast(ray, chunk.PositionWorld, chunk.Blocks, out hitInfo);
        }

        private static bool Raycast(Ray ray, Vector3 chunkPosWorld, Block[,,] blocks, out HitInfo hitInfo)
        {

            hitInfo.position = Vector3.Zero;
            hitInfo.blockPositionIndex = new Vector3Byte(0, 0, 0); // or -1 ????
            hitInfo.normal = Vector3SByte.Zero;
            hitInfo.chunk = null;
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
                    if (!block.IsAir())
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

                                var g = (Vector3)hitInfo.blockPositionIndex -  new Vector3(16,16,16) ;
                                //Debug.Log("g ::" + g);

                                float xA = Math.Abs(g.X);
                                float yA = Math.Abs(g.Y);
                                float zA = Math.Abs(g.Z);

                                if (xA > yA && xA > zA)
                                {
                                    hitInfo.normal = new Vector3SByte(g.X > 0 ? (sbyte)1: (sbyte)-1, (sbyte)0, (sbyte)0);
                                }
                                else
                                {
                                    if(yA > zA)
                                    {
                                        hitInfo.normal = new Vector3SByte((sbyte)0, g.Y > 0 ? (sbyte)1 : (sbyte)-1, (sbyte)0);
                                    }
                                    else
                                    {
                                        hitInfo.normal = new Vector3SByte((sbyte)0,  (sbyte)0, g.Z > 0 ? (sbyte)1 : (sbyte)-1);
                                    }
                                }

                                break;
                        }


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
