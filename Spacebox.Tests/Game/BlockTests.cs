using Spacebox.Game;
using OpenTK.Mathematics;

namespace Spacebox.Tests
{
    public class BlockTests
    {
        [Theory]
        [InlineData(1.2f, 3.5f, 4.8f, 1f, 4f, 5f)]
        [InlineData(-1.7f, 2.3f, 3.499999f, -2f, 2f, 3f)]
        [InlineData(0f, 0f, 0f, 0f, 0f, 0f)]
        [InlineData(2.5f, 2.5f, 2.5f, 2f, 2f, 2f)]
        [InlineData(2.500001f, 2.499999f, 2.500000f, 3f, 2f, 2f)]
        public void RoundVector3_RoundsComponentsCorrectly(float x, float y, float z, float expectedX, float expectedY, float expectedZ)
        {
            var input = new Vector3(x, y, z);
            var result = Block.RoundVector3(input);
            Assert.Equal(expectedX, result.X);
            Assert.Equal(expectedY, result.Y);
            Assert.Equal(expectedZ, result.Z);
        }

        [Theory]
        [InlineData(1f, 0f, 0f, Direction.Right)]
        [InlineData(-1f, 0f, 0f, Direction.Left)]
        [InlineData(0f, 1f, 0f, Direction.Up)]
        [InlineData(0f, -1f, 0f, Direction.Down)]
        [InlineData(0f, 0f, 1f, Direction.Forward)]
        [InlineData(0f, 0f, -1f, Direction.Back)]
        [InlineData(0.999999f, 0f, 0f, Direction.Up)]
        [InlineData(0f, 0.999999f, 0f, Direction.Up)]
        [InlineData(0f, 0f, 0.999999f, Direction.Up)]
        [InlineData(0.000001f, 0f, 0f, Direction.Up)]
        [InlineData(0f, 0.000001f, 0f, Direction.Up)]
        [InlineData(0f, 0f, 0.000001f, Direction.Up)]
        [InlineData(0.5f, 0.5f, 0.5f, Direction.Up)]
        public void GetDirectionFromNormal_ReturnsCorrectDirection(float x, float y, float z, Direction expected)
        {
            var normal = new Vector3(x, y, z);
            var result = Block.GetDirectionFromNormal(normal);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1.2f, 3.5f, 4.8f, 1f, 4f, 5f)]
        [InlineData(-1.7f, 2.3f, 3.499999f, -2f, 2f, 3f)]
        [InlineData(0f, 0f, 0f, 0f, 0f, 0f)]
        [InlineData(2.5f, 2.5f, 2.5f, 2f, 2f, 2f)]
        [InlineData(2.500001f, 2.499999f, 2.500000f, 3f, 2f, 2f)]
        public void RoundVector3_RoundsComponentsCorrectly_WithVariousInputs(float x, float y, float z, float expectedX, float expectedY, float expectedZ)
        {
            var input = new Vector3(x, y, z);
            var result = Block.RoundVector3(input);
            Assert.Equal(expectedX, result.X);
            Assert.Equal(expectedY, result.Y);
            Assert.Equal(expectedZ, result.Z);
        }

        [Theory]
        [InlineData(1f, 0f, 0f, Direction.Right)]
        [InlineData(-1f, 0f, 0f, Direction.Left)]
        [InlineData(0f, 1f, 0f, Direction.Up)]
        [InlineData(0f, -1f, 0f, Direction.Down)]
        [InlineData(0f, 0f, 1f, Direction.Forward)]
        [InlineData(0f, 0f, -1f, Direction.Back)]
        [InlineData(0.999999f, 0f, 0f, Direction.Up)]
        [InlineData(0f, 0.999999f, 0f, Direction.Up)]
        [InlineData(0f, 0f, 0.999999f, Direction.Up)]
        [InlineData(0.000001f, 0f, 0f, Direction.Up)]
        [InlineData(0f, 0.000001f, 0f, Direction.Up)]
        [InlineData(0f, 0f, 0.000001f, Direction.Up)]
        [InlineData(0.5f, 0.5f, 0.5f, Direction.Up)]
        public void GetDirectionFromNormal_ReturnsCorrectDirection_WithVariousNormals(float x, float y, float z, Direction expected)
        {
            var normal = new Vector3(x, y, z);
            var result = Block.GetDirectionFromNormal(normal);
            Assert.Equal(expected, result);
        }
    }
}
