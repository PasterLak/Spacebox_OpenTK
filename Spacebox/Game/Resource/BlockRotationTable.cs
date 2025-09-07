using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Resource
{
    public static class BlockRotationTable
    {
        private static readonly byte[,] DirectionFaceRemap = new byte[6, 6] {
            { 1, 0, 2, 3, 4, 5 },
            { 0, 1, 2, 3, 4, 5 },
            { 2, 3, 1, 0, 4, 5 },
            { 2, 3, 0, 1, 4, 5 },
            { 4, 5, 2, 3, 1, 0 },
            { 4, 5, 2, 3, 0, 1 }
        };

        private static readonly byte[,] RotationFaceRemap = new byte[4, 6] {
            { 0, 1, 2, 3, 4, 5 },
            { 0, 1, 5, 4, 2, 3 },
            { 0, 1, 3, 2, 5, 4 },
            { 0, 1, 4, 5, 3, 2 }
        };

        private static readonly byte[,] DirectionUVRotation = new byte[6, 6] { // dir face  rot type 1 right 2 180g 3 left
            { 2, 2, 2, 2, 2, 2 },
            { 0, 0, 0, 0, 0, 0 },
            { 3, 1, 3, 0, 3, 1 },
            { 1, 3, 3, 0, 1, 3 },
            { 0, 2, 0, 3, 0, 0 },
            { 2, 0, 2, 1, 1, 0 }
        };

        private static readonly byte[,] CombinedRotationTable = new byte[4, 4] {
            { 0, 1, 2, 3 },
            { 1, 2, 3, 0 },
            { 2, 3, 0, 1 },
            { 3, 0, 1, 2 }
        };

        public static Face GetBlockFaceForWorld(Face worldFace, Direction direction, Rotation rotation)
        {
            byte afterDirection = DirectionFaceRemap[(byte)direction, (byte)worldFace];
            return (Face)afterDirection;
        }

        public static byte GetCombinedUVRotation(Face worldFace, Direction direction, Rotation rotation)
        {
            byte baseRotation = DirectionUVRotation[(byte)direction, (byte)worldFace];

            if (worldFace == Face.Down || worldFace == Face.Up)
            {
                byte additionalRotation = (byte)rotation;
                if (direction == Direction.Down)
                {
                    additionalRotation = (byte)((4 - (byte)rotation) % 4);
                }
                return CombinedRotationTable[baseRotation, additionalRotation];
            }

            return baseRotation;
        }

        public static byte GetDirectionFaceRemap(Direction direction, Face face)
        {
            return DirectionFaceRemap[(byte)direction, (byte)face];
        }

        public static byte GetDirectionUVRotation(Direction direction, Face face)
        {
            return DirectionUVRotation[(byte)direction, (byte)face];
        }
    }
}
