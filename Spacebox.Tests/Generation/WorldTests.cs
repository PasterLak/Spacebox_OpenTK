// In your test project (e.g., WorldTests.cs)
using System;
using System.Reflection;
using Xunit;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Engine;
using Spacebox.Game.Player; // Adjust namespaces as necessary
using System.Threading.Tasks;

namespace Spacebox.Tests.Generation
{
    // Mock implementations for dependencies
    public class MockAstronaut : Astronaut
    {
        public override Vector3 Position { get; set; }

        public MockAstronaut() : base(Vector3.Zero)
        {
            // Initialize other necessary properties if required
        }
    }

    public class MockSector : Sector
    {
        public MockSector(Vector3 positionWorld, Vector3i positionIndex, World world)
            : base(positionWorld, positionIndex, world)
        {
        }

        // Implement abstract members if any
    }

    public class WorldTests
    {
        // Constants matching those in the World class
        private const int SizeSectors = 4;
        private const float SectorSizeBlocks = 16f; // Assuming Sector.SizeBlocks = 16
        private const float SectorSizeBlocksHalf = SectorSizeBlocks / 2f;

        // Helper method to invoke private instance methods using reflection
        private object InvokePrivateMethod(object instance, string methodName, params object[] parameters)
        {
            var method = typeof(World).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
                throw new InvalidOperationException($"Method '{methodName}' not found in type '{typeof(World)}'.");

            return method.Invoke(instance, parameters);
        }

        [Fact]
        public void GetSectorPosition_ReturnsCorrectPosition()
        {
            // Arrange
            var player = new MockAstronaut
            {
                Position = new Vector3(0, 0, 0)
            };
            var world = new World(player, null);

            var sectorIndices = new[]
            {
                new Vector3i(0, 0, 0),
                new Vector3i(1, -1, 2),
                new Vector3i(-3, 4, -2),
                new Vector3i(10, 10, 10)
            };

            foreach (var index in sectorIndices)
            {
                // Calculate expected position
                var expectedPosition = new Vector3(
                    index.X * SectorSizeBlocks,
                    index.Y * SectorSizeBlocks,
                    index.Z * SectorSizeBlocks
                );

                // Act
                var actualPositionObj = InvokePrivateMethod(world, "GetSectorPosition", index);
                var actualPosition = (Vector3)actualPositionObj;

                // Assert
                Assert.Equal(expectedPosition, actualPosition);
            }
        }

        [Fact]
        public void GetDistanceToSector_ReturnsCorrectDistance()
        {
            // Arrange
            var player = new MockAstronaut
            {
                Position = new Vector3(10f, 20f, 30f)
            };
            var world = new World(player, null);

            var testCases = new[]
            {
                new
                {
                    SectorPosition = new Vector3(0f, 0f, 0f),
                    ExpectedDistance = Vector3.Distance(
                        new Vector3(SectorSizeBlocksHalf, SectorSizeBlocksHalf, SectorSizeBlocksHalf),
                        new Vector3(10f, 20f, 30f)
                    )
                },
                new
                {
                    SectorPosition = new Vector3(16f, 16f, 16f),
                    ExpectedDistance = Vector3.Distance(
                        new Vector3(16f + SectorSizeBlocksHalf, 16f + SectorSizeBlocksHalf, 16f + SectorSizeBlocksHalf),
                        new Vector3(10f, 20f, 30f)
                    )
                },
                new
                {
                    SectorPosition = new Vector3(-32f, 48f, -16f),
                    ExpectedDistance = Vector3.Distance(
                        new Vector3(-32f + SectorSizeBlocksHalf, 48f + SectorSizeBlocksHalf, -16f + SectorSizeBlocksHalf),
                        new Vector3(10f, 20f, 30f)
                    )
                },
                new
                {
                    SectorPosition = new Vector3(160f, 160f, 160f),
                    ExpectedDistance = Vector3.Distance(
                        new Vector3(160f + SectorSizeBlocksHalf, 160f + SectorSizeBlocksHalf, 160f + SectorSizeBlocksHalf),
                        new Vector3(10f, 20f, 30f)
                    )
                }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var actualDistanceObj = InvokePrivateMethod(world, "GetDistanceToSector", testCase.SectorPosition, player.Position);
                var actualDistance = (float)actualDistanceObj;

                // Assert
                Assert.Equal(testCase.ExpectedDistance, actualDistance, precision: 5);
            }
        }
    }
}
