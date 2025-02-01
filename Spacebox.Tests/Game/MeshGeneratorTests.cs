using System.Reflection;
using Engine;


namespace Spacebox.Tests
{
    /*
    public class MeshGeneratorTests
    {
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
            var vertex = new Vector3SByte(x, y, z);
            var method =
                typeof(MeshGenerator).GetMethod("VectorToBitNumber", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (byte)method.Invoke(null, new object[] { vertex });
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-1, 0, 0, 252)]
        [InlineData(0, -1, 0, 254)]
        [InlineData(0, 0, -1, 255)]
        [InlineData(-1, -1, -1, 255)]
        public void VectorToBitNumber_WithNegativeValues_WrapsAround(sbyte x, sbyte y, sbyte z, byte expected)
        {
            var vertex = new Vector3SByte(x, y, z);
            var method =
                typeof(MeshGenerator).GetMethod("VectorToBitNumber", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (byte)method.Invoke(null, new object[] { vertex });
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ApplyMaskToPosition_ReturnsCorrectResult()
        {
            var position = new Vector3SByte(1, 2, 3);
            var vertex = new Vector3SByte(1, 0, 1);
            byte mask = 5;
            var method =
                typeof(MeshGenerator).GetMethod("ApplyMaskToPosition", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (Vector3SByte[])method.Invoke(null, new object[] { position, vertex, mask });

            Assert.Equal(new Vector3SByte(2, 2, 3), result[0]);
            Assert.Equal(new Vector3SByte(1, 2, 3), result[1]);
            Assert.Equal(new Vector3SByte(1, 2, 4), result[2]);
        }

        [Theory]
        [MemberData(nameof(CombineBitsTestData))]
        public void CombineBits_ReturnsCorrectCombinedValue(byte[] numbers, byte expected)
        {
            var method = typeof(MeshGenerator).GetMethod("CombineBits", BindingFlags.NonPublic | BindingFlags.Static);
            var result = (byte)method.Invoke(null, new object[] { numbers });
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> CombineBitsTestData()
        {
            yield return new object[] { new byte[] { 0, 0, 0 }, 0 };
            yield return new object[] { new byte[] { 1, 2, 4 }, 7 };
            yield return new object[] { new byte[] { 0, 2, 4 }, 6 };
            yield return new object[] { new byte[] { 1, 2, 0 }, 3 };
        }
    }*/
}