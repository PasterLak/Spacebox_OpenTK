// SeedHelperTests.cs
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Xunit;
using Engine.Utils;
using Engine;

namespace Spacebox.Tests
{
    public class SeedHelperTests
    {
        private const int GlobalSeed = 12345;
        private const int SectorRange = 20;
        private const int AsteroidRange = 20;
        private const int ChunkRange = 5;
        private const int MaxIntSeedCollisions = 4;

        [Fact]
        public void SectorIds_AreUniqueOverRange()
        {
            var set = new HashSet<long>();
            for (int x = -SectorRange; x <= SectorRange; x++)
                for (int y = -SectorRange; y <= SectorRange; y++)
                    for (int z = -SectorRange; z <= SectorRange; z++)
                    {
                        long id = SeedHelper.GetSectorId(GlobalSeed, new Vector3i(x, y, z));
                        Assert.True(set.Add(id), $"Duplicate sector ID at ({x},{y},{z})");
                    }
        }

        [Fact]
        public void AsteroidIds_AreUniqueForIntegerGrid()
        {
            long sectorId = SeedHelper.GetSectorId(GlobalSeed, new Vector3i(0, 0, 0));
            var set = new HashSet<long>();
            for (int x = -AsteroidRange; x <= AsteroidRange; x++)
                for (int y = -AsteroidRange; y <= AsteroidRange; y++)
                    for (int z = -AsteroidRange; z <= AsteroidRange; z++)
                    {
                        var pos = new Vector3(x, y, z);
                        long id = SeedHelper.GetAsteroidId(sectorId, pos);
                        Assert.True(set.Add(id), $"Duplicate asteroid ID at ({x},{y},{z})");
                    }
        }

        [Fact]
        public void ChunkIds_AreUniqueOverRange()
        {
            long asteroidId = SeedHelper.GetAsteroidId(
                SeedHelper.GetSectorId(GlobalSeed, new Vector3i(0, 0, 0)),
                new Vector3(0, 0, 0)
            );
            var set = new HashSet<long>();
            for (sbyte x = -ChunkRange; x <= ChunkRange; x++)
                for (sbyte y = -ChunkRange; y <= ChunkRange; y++)
                    for (sbyte z = -ChunkRange; z <= ChunkRange; z++)
                    {
                        var coord = new Vector3SByte(x, y, z);
                        long id = SeedHelper.GetChunkId(asteroidId, coord);
                        Assert.True(set.Add(id), $"Duplicate chunk ID at ({x},{y},{z})");
                    }
        }

        [Fact]
        public void ToIntSeed_ProducesConsistentInt()
        {
            long id = SeedHelper.GetSectorId(GlobalSeed, new Vector3i(1, 2, 3));
            int seed1 = SeedHelper.ToIntSeed(id);
            int seed2 = SeedHelper.ToIntSeed(id);
            Assert.Equal(seed1, seed2);
        }

        [Fact]
        public void SectorSeeds_IntCollisionsAreRare()
        {
            var set = new HashSet<int>();
            int duplicates = 0;
            for (int x = -SectorRange; x <= SectorRange; x++)
                for (int y = -SectorRange; y <= SectorRange; y++)
                    for (int z = -SectorRange; z <= SectorRange; z++)
                    {
                        long id = SeedHelper.GetSectorId(GlobalSeed, new Vector3i(x, y, z));
                        int s = SeedHelper.ToIntSeed(id);
                        if (!set.Add(s)) duplicates++;
                    }
            Assert.True(duplicates <= MaxIntSeedCollisions, $"{duplicates} int sector seed collisions");
        }

        [Fact]
        public void AsteroidSeeds_IntCollisionsAreRare()
        {
            long sectorId = SeedHelper.GetSectorId(GlobalSeed, new Vector3i(0, 0, 0));
            var set = new HashSet<int>();
            int duplicates = 0;
            for (int x = -AsteroidRange; x <= AsteroidRange; x++)
                for (int y = -AsteroidRange; y <= AsteroidRange; y++)
                    for (int z = -AsteroidRange; z <= AsteroidRange; z++)
                    {
                        var pos = new Vector3(x, y, z);
                        long id = SeedHelper.GetAsteroidId(sectorId, pos);
                        int s = SeedHelper.ToIntSeed(id);
                        if (!set.Add(s)) duplicates++;
                    }
            Assert.True(duplicates <= MaxIntSeedCollisions, $"{duplicates} int asteroid seed collisions");
        }

        [Fact]
        public void ChunkSeeds_IntUniqueOverRange()
        {
            long asteroidId = SeedHelper.GetAsteroidId(
                SeedHelper.GetSectorId(GlobalSeed, new Vector3i(0, 0, 0)),
                new Vector3(0, 0, 0)
            );
            var set = new HashSet<int>();
            for (sbyte x = -ChunkRange; x <= ChunkRange; x++)
                for (sbyte y = -ChunkRange; y <= ChunkRange; y++)
                    for (sbyte z = -ChunkRange; z <= ChunkRange; z++)
                    {
                        var coord = new Vector3SByte(x, y, z);
                        long id = SeedHelper.GetChunkId(asteroidId, coord);
                        int s = SeedHelper.ToIntSeed(id);
                        Assert.True(set.Add(s), $"Duplicate int chunk seed at ({x},{y},{z})");
                    }
        }
    }
}
