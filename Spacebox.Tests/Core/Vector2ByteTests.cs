using OpenTK.Mathematics;

namespace Spacebox.Tests
{
    public class Vector2ByteTests
    {
        [Fact]
        public void Constructor_WithBytes_SetsPropertiesCorrectly()
        {
            var vector = new Vector2Byte(10, 20);
            Assert.Equal(10, vector.X);
            Assert.Equal(20, vector.Y);
        }

        [Fact]
        public void Constructor_WithInts_SetsPropertiesCorrectly()
        {
            var vector = new Vector2Byte(300, -10);
            Assert.Equal(44, vector.X);
            Assert.Equal(246, vector.Y);
        }

        [Fact]
        public void Constructor_WithVector2i_SetsPropertiesCorrectly()
        {
            var vec2i = new Vector2i(50, 150);
            var vector = new Vector2Byte(vec2i);
            Assert.Equal(50, vector.X);
            Assert.Equal(150, vector.Y);
        }

        [Fact]
        public void Constructor_WithVector2_SetsPropertiesCorrectly()
        {
            var vec2 = new Vector2(25.5f, 125.7f);
            var vector = new Vector2Byte(vec2);
            Assert.Equal(25, vector.X);
            Assert.Equal(125, vector.Y);
        }

        [Fact]
        public void StaticProperty_Zero_ReturnsCorrectVector()
        {
            var vector = Vector2Byte.Zero;
            Assert.Equal(0, vector.X);
            Assert.Equal(0, vector.Y);
        }

        [Fact]
        public void StaticProperty_One_ReturnsCorrectVector()
        {
            var vector = Vector2Byte.One;
            Assert.Equal(1, vector.X);
            Assert.Equal(1, vector.Y);
        }

        [Fact]
        public void StaticProperty_Right_ReturnsCorrectVector()
        {
            var vector = Vector2Byte.Right;
            Assert.Equal(1, vector.X);
            Assert.Equal(0, vector.Y);
        }

        [Fact]
        public void StaticProperty_Up_ReturnsCorrectVector()
        {
            var vector = Vector2Byte.Up;
            Assert.Equal(0, vector.X);
            Assert.Equal(1, vector.Y);
        }

        [Fact]
        public void EqualityOperator_ReturnsTrue_ForEqualVectors()
        {
            var v1 = new Vector2Byte(100, 150);
            var v2 = new Vector2Byte(100, 150);
            Assert.True(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_ReturnsFalse_ForDifferentVectors()
        {
            var v1 = new Vector2Byte(100, 150);
            var v2 = new Vector2Byte(101, 150);
            Assert.False(v1 == v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsTrue_ForDifferentVectors()
        {
            var v1 = new Vector2Byte(100, 150);
            var v2 = new Vector2Byte(101, 150);
            Assert.True(v1 != v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsFalse_ForEqualVectors()
        {
            var v1 = new Vector2Byte(100, 150);
            var v2 = new Vector2Byte(100, 150);
            Assert.False(v1 != v2);
        }

        [Fact]
        public void Addition_WithNoOverflow_ReturnsCorrectResult()
        {
            var v1 = new Vector2Byte(10, 20);
            var v2 = new Vector2Byte(5, 15);
            var result = v1 + v2;
            Assert.Equal(15, result.X);
            Assert.Equal(35, result.Y);
        }

        [Fact]
        public void Addition_WithOverflow_ReturnsMaxValue()
        {
            var v1 = new Vector2Byte(250, 250);
            var v2 = new Vector2Byte(10, 10);
            var result = v1 + v2;
            Assert.Equal(255, result.X);
            Assert.Equal(255, result.Y);
        }

        [Fact]
        public void Subtraction_WithNoUnderflow_ReturnsCorrectResult()
        {
            var v1 = new Vector2Byte(50, 60);
            var v2 = new Vector2Byte(20, 30);
            var result = v1 - v2;
            Assert.Equal(30, result.X);
            Assert.Equal(30, result.Y);
        }

        [Fact]
        public void Subtraction_WithUnderflow_ReturnsZero()
        {
            var v1 = new Vector2Byte(10, 20);
            var v2 = new Vector2Byte(20, 30);
            var result = v1 - v2;
            Assert.Equal(0, result.X);
            Assert.Equal(0, result.Y);
        }

        [Fact]
        public void ExplicitCast_ToVector2i_ReturnsCorrectVector2i()
        {
            var v = new Vector2Byte(10, 20);
            Vector2i vec2i = (Vector2i)v;
            Assert.Equal(10, vec2i.X);
            Assert.Equal(20, vec2i.Y);
        }

        [Fact]
        public void ExplicitCast_FromVector2i_ReturnsCorrectVector2Byte()
        {
            var vec2i = new Vector2i(40, 50);
            Vector2Byte v = (Vector2Byte)vec2i;
            Assert.Equal(40, v.X);
            Assert.Equal(50, v.Y);
        }

        [Fact]
        public void ExplicitCast_ToVector2_ReturnsCorrectVector2()
        {
            var v = new Vector2Byte(70, 80);
            Vector2 vec2 = (Vector2)v;
            Assert.Equal(70f, vec2.X);
            Assert.Equal(80f, vec2.Y);
        }

        [Fact]
        public void ExplicitCast_FromVector2_ReturnsCorrectVector2Byte()
        {
            var vec2 = new Vector2(100f, 110f);
            Vector2Byte v = (Vector2Byte)vec2;
            Assert.Equal(100, v.X);
            Assert.Equal(110, v.Y);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 4, 5)]
        [InlineData(5, 12, 13)]
        [InlineData(8, 15, 17)]
        public void Distance_ReturnsCorrectValue(byte x1, byte y1, ushort expected)
        {
            var a = new Vector2Byte(x1, y1);
            var b = Vector2Byte.Zero;
            ushort distance = Vector2Byte.Distance(a, b);
            Assert.Equal(expected, distance);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(3, 4, 25)]
        [InlineData(5, 12, 169)]
        [InlineData(8, 15, 289)]
        public void DistanceSquared_ReturnsCorrectValue(byte x1, byte y1, ushort expected)
        {
            var a = new Vector2Byte(x1, y1);
            var b = Vector2Byte.Zero;
            ushort distanceSquared = Vector2Byte.DistanceSquared(a, b);
            Assert.Equal(expected, distanceSquared);
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            var vector = new Vector2Byte(1, 2);
            string str = vector.ToString();
            Assert.Equal("(1,2)", str);
        }
    }
}
