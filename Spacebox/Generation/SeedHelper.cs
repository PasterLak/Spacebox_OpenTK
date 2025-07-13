
using OpenTK.Mathematics;

namespace Engine.Utils
{
    public static class SeedHelper
    {
        static ulong Mix(ulong x)
        {
            x ^= x >> 30;
            x *= 0xbf58476d1ce4e5b9UL;
            x ^= x >> 27;
            x *= 0x94d049bb133111ebUL;
            x ^= x >> 31;
            return x;
        }

        public static ulong GetSectorId(int globalSeed, Vector3i sectorIndex)
        {
            ulong h = (uint)globalSeed;
            h = Mix(h ^ (uint)sectorIndex.X);
            h = Mix(h ^ (uint)sectorIndex.Y);
            h = Mix(h ^ (uint)sectorIndex.Z);
            return Mix(h);
        }

        public static ulong GetAsteroidId(ulong sectorId, Vector3 positionInSector)
        {
            int xi = (int)MathF.Round(positionInSector.X);
            int yi = (int)MathF.Round(positionInSector.Y);
            int zi = (int)MathF.Round(positionInSector.Z);
            ulong h = sectorId;
            h = Mix(h ^ (uint)xi);
            h = Mix(h ^ (uint)yi);
            h = Mix(h ^ (uint)zi);
            return Mix(h);
        }

        public static ulong GetChunkId(ulong asteroidId, Vector3SByte chunkCoord)
        {
            ulong h = asteroidId;
            h = Mix(h ^ (byte)chunkCoord.X);
            h = Mix(h ^ (byte)chunkCoord.Y);
            h = Mix(h ^ (byte)chunkCoord.Z);
            return Mix(h);
        }

        public static int GetChunkIdInt(ulong asteroidId, Vector3SByte chunkCoord)
        {
            return ToIntSeed(GetChunkId(asteroidId, chunkCoord));
        }

        public static int ToIntSeed(ulong id)
        {
            return (int)(id & 0xFFFFFFFFu);
        }
    }
}
