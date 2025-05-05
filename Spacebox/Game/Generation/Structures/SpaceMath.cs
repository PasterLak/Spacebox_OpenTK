

using OpenTK.Mathematics;
using System.Drawing;

namespace Spacebox.Game.Generation.Structures
{
    public static class SpaceMath
    {

        public static class World
        {
            public static bool IsInside(Vector3 posWorld, Generation.World world)
            {
                return false;
            }

            public static class Sector
            {
                public static bool IsInside(Vector3 posWorld, Generation.World world)
                {
                    return false;
                }
            }

            public static class SpaceEntity
            {
                public static bool IsInside(Vector3 posWorld, Generation.SpaceEntity spaceEntity)
                {
                    const short size = Generation.SpaceEntity.SizeBlocksHalf;

                    Vector3 local = posWorld - spaceEntity.PositionWorld;
                    return local.X >= -size && local.X < size
                        && local.Y >= -size && local.Y < size
                        && local.Z >= -size && local.Z < size;
                }
            }

            public static class Chunk
            {

            }
        }
    }
}