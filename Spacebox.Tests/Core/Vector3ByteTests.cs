using OpenTK.Mathematics;

namespace Spacebox.Tests
{
    public class Vector3ByteTests
    {
        [Fact]
        public void Constructor_WithBytes_SetsPropertiesCorrectly()
        {
            var vector = new Vector3Byte(10, 20, 30);
            Assert.Equal(10, vector.X);
            Assert.Equal(20, vector.Y);
            Assert.Equal(30, vector.Z);
        }

        [Fact]
        public void Constructor_WithInts_SetsPropertiesCorrectly()
        {
            var vector = new Vector3Byte(100, 200, 300);
            Assert.Equal(100, vector.X);
            Assert.Equal(200, vector.Y);
            Assert.Equal(44, vector.Z); // 300 mod 256 = 44
        }

        [Fact]
        public void Constructor_WithVector3i_SetsPropertiesCorrectly()
        {
            var vec3i = new Vector3i(50, 150, 250);
            var vector = new Vector3Byte(vec3i);
            Assert.Equal(50, vector.X);
            Assert.Equal(150, vector.Y);
            Assert.Equal(250, vector.Z);
        }

        [Fact]
        public void Constructor_WithVector3_SetsPropertiesCorrectly()
        {
            var vec3 = new Vector3(25.5f, 125.7f, 255.9f);
            var vector = new Vector3Byte(vec3);
            Assert.Equal(25, vector.X);
            Assert.Equal(125, vector.Y);
            Assert.Equal(255, vector.Z);
        }

        [Fact]
        public void StaticProperty_One_ReturnsCorrectVector()
        {
            var vector = Vector3Byte.One;
            Assert.Equal(1, vector.X);
            Assert.Equal(1, vector.Y);
            Assert.Equal(1, vector.Z);
        }

        [Fact]
        public void StaticProperty_Zero_ReturnsCorrectVector()
        {
            var vector = Vector3Byte.Zero;
            Assert.Equal(0, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void StaticProperty_Right_ReturnsCorrectVector()
        {
            var vector = Vector3Byte.Right;
            Assert.Equal(1, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void StaticProperty_Forward_ReturnsCorrectVector()
        {
            var vector = Vector3Byte.Forward;
            Assert.Equal(0, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(1, vector.Z);
        }

        [Fact]
        public void StaticProperty_Up_ReturnsCorrectVector()
        {
            var vector = Vector3Byte.Up;
            Assert.Equal(0, vector.X);
            Assert.Equal(1, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void Addition_WithNoOverflow_ReturnsCorrectResult()
        {
            var v1 = new Vector3Byte(10, 20, 30);
            var v2 = new Vector3Byte(5, 15, 25);
            var result = v1 + v2;
            Assert.Equal(15, result.X);
            Assert.Equal(35, result.Y);
            Assert.Equal(55, result.Z);
        }

        [Fact]
        public void Addition_WithOverflow_ReturnsMaxValue()
        {
            var v1 = new Vector3Byte(250, 250, 250);
            var v2 = new Vector3Byte(10, 10, 10);
            var result = v1 + v2;
            Assert.Equal(255, result.X);
            Assert.Equal(255, result.Y);
            Assert.Equal(255, result.Z);
        }

        [Fact]
        public void Subtraction_WithNoUnderflow_ReturnsCorrectResult()
        {
            var v1 = new Vector3Byte(50, 60, 70);
            var v2 = new Vector3Byte(20, 30, 40);
            var result = v1 - v2;
            Assert.Equal(30, result.X);
            Assert.Equal(30, result.Y);
            Assert.Equal(30, result.Z);
        }

        [Fact]
        public void Subtraction_WithUnderflow_ReturnsZero()
        {
            var v1 = new Vector3Byte(10, 20, 30);
            var v2 = new Vector3Byte(20, 30, 40);
            var result = v1 - v2;
            Assert.Equal(0, result.X);
            Assert.Equal(0, result.Y);
            Assert.Equal(0, result.Z);
        }

        [Fact]
        public void EqualityOperator_ReturnsTrue_ForEqualVectors()
        {
            var v1 = new Vector3Byte(100, 150, 200);
            var v2 = new Vector3Byte(100, 150, 200);
            Assert.True(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_ReturnsFalse_ForDifferentVectors()
        {
            var v1 = new Vector3Byte(100, 150, 200);
            var v2 = new Vector3Byte(101, 150, 200);
            Assert.False(v1 == v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsTrue_ForDifferentVectors()
        {
            var v1 = new Vector3Byte(100, 150, 200);
            var v2 = new Vector3Byte(101, 150, 200);
            Assert.True(v1 != v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsFalse_ForEqualVectors()
        {
            var v1 = new Vector3Byte(100, 150, 200);
            var v2 = new Vector3Byte(100, 150, 200);
            Assert.False(v1 != v2);
        }

        [Fact]
        public void ExplicitCast_ToVector3i_ReturnsCorrectVector3i()
        {
            var v = new Vector3Byte(10, 20, 30);
            Vector3i vec3i = (Vector3i)v;
            Assert.Equal(10, vec3i.X);
            Assert.Equal(20, vec3i.Y);
            Assert.Equal(30, vec3i.Z);
        }

        [Fact]
        public void ExplicitCast_FromVector3i_ReturnsCorrectVector3Byte()
        {
            var vec3i = new Vector3i(40, 50, 60);
            Vector3Byte v = (Vector3Byte)vec3i;
            Assert.Equal(40, v.X);
            Assert.Equal(50, v.Y);
            Assert.Equal(60, v.Z);
        }

        [Fact]
        public void ExplicitCast_ToVector3_ReturnsCorrectVector3()
        {
            var v = new Vector3Byte(70, 80, 90);
            Vector3 vec3 = (Vector3)v;
            Assert.Equal(70f, vec3.X);
            Assert.Equal(80f, vec3.Y);
            Assert.Equal(90f, vec3.Z);
        }

        [Fact]
        public void ExplicitCast_FromVector3_ReturnsCorrectVector3Byte()
        {
            var vec3 = new Vector3(100f, 110f, 120f);
            Vector3Byte v = (Vector3Byte)vec3;
            Assert.Equal(100, v.X);
            Assert.Equal(110, v.Y);
            Assert.Equal(120, v.Z);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(3, 4, 0, 5)]
        [InlineData(5, 12, 0, 13)]
        [InlineData(8, 15, 0, 17)]
        public void Distance_ReturnsCorrectValue(byte x1, byte y1, byte z1, ushort expected)
        {
            var a = new Vector3Byte(x1, y1, z1);
            var b = Vector3Byte.Zero;
            ushort distance = Vector3Byte.Distance(a, b);
            Assert.Equal(expected, distance);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(3, 4, 0, 25)]
        [InlineData(5, 12, 0, 169)]
        [InlineData(8, 15, 0, 289)]
        public void DistanceSquared_ReturnsCorrectValue(byte x1, byte y1, byte z1, ushort expected)
        {
            var a = new Vector3Byte(x1, y1, z1);
            var b = Vector3Byte.Zero;
            ushort distanceSquared = Vector3Byte.DistanceSquared(a, b);
            Assert.Equal(expected, distanceSquared);
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            var vector = new Vector3Byte(1, 2, 3);
            string str = vector.ToString();
            Assert.Equal("(1,2,3)", str);
        }
    }
}
