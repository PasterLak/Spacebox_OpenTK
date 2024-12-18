using Spacebox.Game;
using Spacebox.Common;
using System.Reflection;

namespace Spacebox.Tests
{
    public class AOVoxelsTests
    {
        #region VectorToBitNumber Tests

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 0, 0, 4)]
        [InlineData(0, 1, 0, 2)]
        [InlineData(0, 0, 1, 1)]
        [InlineData(1, 1, 0, 6)]
        [InlineData(1, 0, 1, 5)]
        [InlineData(0, 1, 1, 3)]
        [InlineData(1, 1, 1, 7)]
        public void VectorToBitNumber_ReturnsCorrectBitNumber(sbyte x, sbyte y, sbyte z, byte expected)
        {
            // Arrange
            var vertex = new Vector3SByte(x, y, z);
            var method = typeof(AOVoxels).GetMethod("VectorToBitNumber", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // Ensure the method exists

            // Act
            var result = (byte)method.Invoke(null, new object[] { vertex });

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-1, 0, 0, 252)]
        [InlineData(0, -1, 0, 254)]
        [InlineData(0, 0, -1, 255)]
        [InlineData(-1, -1, -1, 255)]
        public void VectorToBitNumber_WithNegativeValues_WrapsAround(sbyte x, sbyte y, sbyte z, byte expected)
        {
            // Arrange
            var vertex = new Vector3SByte(x, y, z);
            var method = typeof(AOVoxels).GetMethod("VectorToBitNumber", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // Ensure the method exists

            // Act
            var result = (byte)method.Invoke(null, new object[] { vertex });

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region CombineBits Tests

        [Theory]
        [MemberData(nameof(CombineBitsTestData))]
        public void CombineBits_ReturnsCorrectCombinedValue(byte[] numbers, byte expected)
        {
            // Arrange
            var method = typeof(AOVoxels).GetMethod("CombineBits", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // Ensure the method exists

            // Act
            var result = (byte)method.Invoke(null, new object[] { numbers });

            // Assert
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> CombineBitsTestData()
        {
            yield return new object[] { new byte[] { 0, 0, 0 }, 0 };
            yield return new object[] { new byte[] { 1, 2, 4 }, 7 };
            yield return new object[] { new byte[] { 0, 2, 4 }, 6 };
            yield return new object[] { new byte[] { 1, 2, 0 }, 3 };
        }

        #endregion

        #region ApplyMaskToPosition Tests

        [Fact]
        public void ApplyMaskToPosition_ReturnsCorrectResult_WithPositiveNormal()
        {
            // Arrange
            var position = new Vector3SByte(0,0,0);
            var vertex = new Vector3SByte(0,0,1);
            byte mask = 7; // Binary 101
            var normal = new Vector3SByte(0, 0, 1); // Assuming normal (0,0,1)
            var method = typeof(AOVoxels).GetMethod("ApplyMaskToPosition", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // Ensure the method exists

            // Act
            var result = (Vector3SByte[])method.Invoke(null, new object[] { position, vertex, mask, normal });

            var expected = new List<Vector3SByte>
            {
                new Vector3SByte(-1,0,0),
                new Vector3SByte(0,-1,0)
            };

            Assert.Equal(expected.Count, result.Length);
            foreach (var vec in expected)
            {
                Assert.Contains(result, r => r.Equals(vec));
            }
        }

        [Fact]
        public void ApplyMaskToPosition_ReturnsCorrectResult_WithNegativeNormal()
        {
            // Arrange
            var position = new Vector3SByte(0,0,0);
            var vertex = new Vector3SByte(0,0,0);
            byte mask = 6; // Binary 110
            var normal = new Vector3SByte(0, 0, -1); // Assuming normal (0,0,-1)
            var method = typeof(AOVoxels).GetMethod("ApplyMaskToPosition", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); // Ensure the method exists

            // Act
            var result = (Vector3SByte[])method.Invoke(null, new object[] { position, vertex, mask, normal });

            // Assert

            var expected = new List<Vector3SByte>
            {
                new Vector3SByte(-1,0,0),
                new Vector3SByte(0,-1,0)
                
            };

            Assert.Equal(expected.Count, result.Length);
            foreach (var vec in expected)
            {
                Assert.Contains(result, r => r.Equals(vec));
            }
        }

        #endregion

        #region GetNeigbordPositions Tests

        [Fact]
        public void GetNeigbordPositions_ReturnsCorrectNeighbors_AfterInit()
        {
            // Arrange
            AOVoxels.Init(); 
            var face = Face.Front; 
            var vertex = new Vector3SByte(1, 0, 1); 

            // Act
            var neighbors = AOVoxels.GetNeigbordPositions(face, vertex);

            // Assert
           
            Assert.NotNull(neighbors);
            Assert.NotEmpty(neighbors);
        }

        #endregion

     
    }
}
