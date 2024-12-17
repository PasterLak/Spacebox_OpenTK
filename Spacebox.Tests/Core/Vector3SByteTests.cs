using Spacebox.Common;
using OpenTK.Mathematics;

namespace Spacebox.Tests
{
    public class Vector3SByteTests
    {
        [Fact]
        public void Constructor_WithSBytes_SetsPropertiesCorrectly()
        {
            var vector = new Vector3SByte(10, 20, 30);
            Assert.Equal(10, vector.X);
            Assert.Equal(20, vector.Y);
            Assert.Equal(30, vector.Z);
        }

        [Fact]
        public void Constructor_WithInts_SetsPropertiesCorrectly()
        {
            var vector = new Vector3SByte(300, -10, 127);
            Assert.Equal(44, vector.X);
            Assert.Equal(-10, vector.Y);
            Assert.Equal(127, vector.Z);
        }

        [Fact]
        public void Constructor_WithVector3i_SetsPropertiesCorrectly()
        {
            var vec3i = new Vector3i(50, -60, 70);
            var vector = new Vector3SByte(vec3i);
            Assert.Equal(50, vector.X);
            Assert.Equal(-60, vector.Y);
            Assert.Equal(70, vector.Z);
        }

        [Fact]
        public void Constructor_WithVector3_SetsPropertiesCorrectly()
        {
            var vec3 = new Vector3(25.5f, -125.7f, 255.9f);
            var vector = new Vector3SByte(vec3);
            Assert.Equal(25, vector.X);
            Assert.Equal(-125, vector.Y);
            Assert.Equal(-1, vector.Z);
        }

        [Fact]
        public void StaticProperty_Zero_ReturnsCorrectVector()
        {
            var vector = Vector3SByte.Zero;
            Assert.Equal(0, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void StaticProperty_One_ReturnsCorrectVector()
        {
            var vector = Vector3SByte.One;
            Assert.Equal(1, vector.X);
            Assert.Equal(1, vector.Y);
            Assert.Equal(1, vector.Z);
        }

        [Fact]
        public void StaticProperty_Right_ReturnsCorrectVector()
        {
            var vector = Vector3SByte.Right;
            Assert.Equal(1, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void StaticProperty_Up_ReturnsCorrectVector()
        {
            var vector = Vector3SByte.Up;
            Assert.Equal(0, vector.X);
            Assert.Equal(1, vector.Y);
            Assert.Equal(0, vector.Z);
        }

        [Fact]
        public void StaticProperty_Forward_ReturnsCorrectVector()
        {
            var vector = Vector3SByte.Forward;
            Assert.Equal(0, vector.X);
            Assert.Equal(0, vector.Y);
            Assert.Equal(1, vector.Z);
        }

        [Fact]
        public void CreateFrom_Vector3_ReturnsCorrectVector()
        {
            var vec3 = new Vector3(1.5f, -2.5f, 3.5f);
            var vector = Vector3SByte.CreateFrom(vec3);
            Assert.Equal(1, vector.X);
            Assert.Equal(-2, vector.Y);
            Assert.Equal(3, vector.Z);
        }

        [Fact]
        public void Addition_WithNoOverflow_ReturnsCorrectResult()
        {
            var v1 = new Vector3SByte(10, 20, 30);
            var v2 = new Vector3SByte(5, 15, 25);
            var result = v1 + v2;
            Assert.Equal(15, result.X);
            Assert.Equal(35, result.Y);
            Assert.Equal(55, result.Z);
        }

        [Fact]
        public void Addition_WithOverflow_ReturnsClampedMaxValue()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(30, 50, 60);
            var result = v1 + v2;
            Assert.Equal(sbyte.MaxValue, result.X);
            Assert.Equal(sbyte.MaxValue, result.Y);
            Assert.Equal(sbyte.MaxValue, result.Z);
        }

        [Fact]
        public void Subtraction_WithNoUnderflow_ReturnsCorrectResult()
        {
            var v1 = new Vector3SByte(50, 60, 70);
            var v2 = new Vector3SByte(20, 30, 40);
            var result = v1 - v2;
            Assert.Equal(30, result.X);
            Assert.Equal(30, result.Y);
            Assert.Equal(30, result.Z);
        }

        [Fact]
        public void Subtraction_WithUnderflow_ReturnsClampedMinValue()
        {
            var v1 = new Vector3SByte(-90, -20, -30);
            var v2 = new Vector3SByte(90, 120, 120);
            var result = v1 - v2;
            Assert.Equal(sbyte.MinValue, result.X);
            Assert.Equal(sbyte.MinValue, result.Y);
            Assert.Equal(sbyte.MinValue, result.Z);
        }

        [Fact]
        public void EqualityOperator_ReturnsTrue_ForEqualVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(100, 100, 100);
            Assert.True(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_ReturnsFalse_ForDifferentVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(101, 100, 100);
            Assert.False(v1 == v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsTrue_ForDifferentVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(101, 100, 100);
            Assert.True(v1 != v2);
        }

        [Fact]
        public void InequalityOperator_ReturnsFalse_ForEqualVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(100, 100, 100);
            Assert.False(v1 != v2);
        }

        [Fact]
        public void ExplicitCast_ToVector3i_ReturnsCorrectVector3i()
        {
            var v = new Vector3SByte(10, 20, 30);
            Vector3i vec3i = (Vector3i)v;
            Assert.Equal(10, vec3i.X);
            Assert.Equal(20, vec3i.Y);
            Assert.Equal(30, vec3i.Z);
        }

        [Fact]
        public void ExplicitCast_FromVector3i_ReturnsCorrectVector3SByte()
        {
            var vec3i = new Vector3i(40, -50, 60);
            Vector3SByte v = (Vector3SByte)vec3i;
            Assert.Equal(40, v.X);
            Assert.Equal(-50, v.Y);
            Assert.Equal(60, v.Z);
        }

        [Fact]
        public void ExplicitCast_ToVector3_ReturnsCorrectVector3()
        {
            var v = new Vector3SByte(70, 80, 90);
            Vector3 vec3 = (Vector3)v;
            Assert.Equal(70f, vec3.X);
            Assert.Equal(80f, vec3.Y);
            Assert.Equal(90f, vec3.Z);
        }

        [Fact]
        public void ExplicitCast_FromVector3_ReturnsCorrectVector3SByte()
        {
            var vec3 = new Vector3(100f, -110f, 120f);
            Vector3SByte v = (Vector3SByte)vec3;
            Assert.Equal(100, v.X);
            Assert.Equal(-110, v.Y);
            Assert.Equal(120, v.Z);
        }

        [Theory]
        [InlineData(0, 0, 0, 0.0)]
        [InlineData(3, 4, 0, 5.0)]
        [InlineData(5, 12, 0, 13.0)]
        [InlineData(8, 15, 0, 17.0)]
        public void Distance_ReturnsCorrectValue(sbyte x1, sbyte y1, sbyte z1, double expected)
        {
            var a = new Vector3SByte(x1, y1, z1);
            var b = Vector3SByte.Zero;
            double distance = Vector3SByte.Distance(a, b);
            Assert.Equal(expected, distance);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(3, 4, 0, 25)]
        [InlineData(5, 12, 0, 169)]
        [InlineData(8, 15, 0, 289)]
        public void DistanceSquared_ReturnsCorrectValue(sbyte x1, sbyte y1, sbyte z1, int expected)
        {
            var a = new Vector3SByte(x1, y1, z1);
            var b = Vector3SByte.Zero;
            int distanceSquared = Vector3SByte.DistanceSquared(a, b);
            Assert.Equal(expected, distanceSquared);
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            var vector = new Vector3SByte(1, 2, 3);
            string str = vector.ToString();
            Assert.Equal("(1,2,3)", str);
        }

        [Fact]
        public void Equals_ReturnsTrue_ForEqualVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(100, 100, 100);
            Assert.True(v1.Equals(v2));
        }

        [Fact]
        public void Equals_ReturnsFalse_ForDifferentVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(101, 100, 100);
            Assert.False(v1.Equals(v2));
        }

        [Fact]
        public void GetHashCode_ReturnsSameHashCode_ForEqualVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(100, 100, 100);
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ReturnsDifferentHashCode_ForDifferentVectors()
        {
            var v1 = new Vector3SByte(100, 100, 100);
            var v2 = new Vector3SByte(101, 100, 100);
            Assert.NotEqual(v1.GetHashCode(), v2.GetHashCode());
        }
    }
}
