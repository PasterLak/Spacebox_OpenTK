using OpenTK.Mathematics;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;


namespace Spacebox.Game.Physics
{
    public class VoxelPhysics
    {
        public static bool IsColliding(BoundingVolume volume, Chunk chunk, Vector3 Position)
        {
            return IsColliding(volume, chunk.Blocks, Position);
        }
        public static bool IsColliding(BoundingVolume volume, Block[,,] Blocks, Vector3 Position)
        {

            BoundingSphere sphere = volume as BoundingSphere;
            if (sphere == null)
            {

                return false;
            }

            Vector3 sphereMin = sphere.Center - new Vector3(sphere.Radius);
            Vector3 sphereMax = sphere.Center + new Vector3(sphere.Radius);


            Vector3 localMin = sphereMin - Position;
            Vector3 localMax = sphereMax - Position;


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
                        if (!block.IsAir())
                        {

                            Vector3 blockMin = Position + new Vector3(x, y, z);
                            BoundingBox blockBox = new BoundingBox(blockMin + new Vector3(0.5f), Vector3.One);


                            if (sphere.Intersects(blockBox))
                            {
                                return true;
                            }

                        }
                    }
                }
            }

            return false;
        }

        public struct HitInfo
        {
            public Vector3 position;
            public Vector3i blockPosition;
            public Vector3 normal;
        }

        public static bool Raycast(Ray ray, Vector3 Position, Chunk chunk, out HitInfo hitInfo)
        {
            return Raycast(ray, Position, chunk, out hitInfo);
        }
        public static bool Raycast(Ray ray, Vector3 Position, Block[,,] blocks, out HitInfo hitInfo)
        {

            hitInfo.position = Vector3.Zero;
            hitInfo.blockPosition = new Vector3i(-1, -1, -1);
            hitInfo.normal = Vector3.Zero;

            // if (!_isLoadedOrGenerated) return false;

            Vector3 rayOrigin = ray.Origin - Position;
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
                        hitInfo.blockPosition = new Vector3i(x, y, z);
                        hitInfo.position = ray.Origin + ray.Direction * distanceTraveled;

                        switch (side)
                        {
                            case 0:
                                hitInfo.normal = new Vector3(-stepX, 0, 0);
                                break;
                            case 1:
                                hitInfo.normal = new Vector3(0, -stepY, 0);
                                break;
                            case 2:
                                hitInfo.normal = new Vector3(0, 0, -stepZ);
                                break;
                        }

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

        private static bool IsInRange(int x, int y, int z)
        {
            return x >= 0 && x < Chunk.Size && y >= 0 && y < Chunk.Size && z >= 0 && z < Chunk.Size;
        }
    }
}
