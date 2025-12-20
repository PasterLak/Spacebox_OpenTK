using System;
using OpenTK.Mathematics;

namespace Engine.Utils
{
    public static class SeedHelper
    {
        // procedural IDs are always non-negative
        private const long PROCEDURAL_MASK = 0x7FFFFFFFFFFFFFFF;

        static ulong Mix(ulong x)
        {
            x ^= x >> 30;
            x *= 0xbf58476d1ce4e5b9UL;
            x ^= x >> 27;
            x *= 0x94d049bb133111ebUL;
            x ^= x >> 31;
            return x;
        }


        public static long GetSectorId(int globalSeed, Vector3i sectorIndex)
        {
            ulong h = (uint)globalSeed;
            h = Mix(h ^ (uint)sectorIndex.X);
            h = Mix(h ^ (uint)sectorIndex.Y);
            h = Mix(h ^ (uint)sectorIndex.Z);

            return (long)Mix(h);
        }
        // always positive ID
        public static long GetAsteroidId(long sectorId, Vector3 positionInSector)
        {
            int xi = (int)MathF.Round(positionInSector.X);
            int yi = (int)MathF.Round(positionInSector.Y);
            int zi = (int)MathF.Round(positionInSector.Z);

            ulong h = (ulong)sectorId;

            h = Mix(h ^ (uint)xi);
            h = Mix(h ^ (uint)yi);
            h = Mix(h ^ (uint)zi);

            return (long)(Mix(h) & (ulong)PROCEDURAL_MASK);
        }

        public static long GetChunkId(long asteroidId, Vector3SByte chunkCoord)
        {
            ulong h = (ulong)asteroidId;
            h = Mix(h ^ (byte)chunkCoord.X);
            h = Mix(h ^ (byte)chunkCoord.Y);
            h = Mix(h ^ (byte)chunkCoord.Z);

            return (long)(Mix(h));
        }

        public static int GetChunkIdInt(long asteroidId, Vector3SByte chunkCoord)
        {
            return ToIntSeed(GetChunkId(asteroidId, chunkCoord));
        }

        public static int ToIntSeed(long id)
        {
            return (int)id;
        }

        private static readonly Random _dynamicRandom = new Random();

        // always negative dynamic entity ID
        public static long GenerateDynamicEntityId()
        {
            byte[] buf = new byte[8];
            _dynamicRandom.NextBytes(buf);
            long val = BitConverter.ToInt64(buf, 0);

            // (long.MinValue ... -1)
            return val | unchecked((long)0x8000000000000000);
        }
    }
}