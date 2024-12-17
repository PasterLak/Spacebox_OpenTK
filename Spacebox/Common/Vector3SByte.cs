using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public struct Vector3SByte : IEquatable<Vector3SByte>
    {
       
        public sbyte X { get; set; }

       
        public sbyte Y { get; set; }

      
        public sbyte Z { get; set; }

        public Vector3SByte(sbyte x, sbyte y, sbyte z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3SByte(int x, int y, int z)
        {
            this.X = (sbyte)x;
            this.Y = (sbyte)y;
            this.Z = (sbyte)z;
        }

        public Vector3SByte(Vector3i v)
        {
            this.X = (sbyte)v.X;
            this.Y = (sbyte)v.Y;
            this.Z = (sbyte)v.Z;
        }

        public Vector3SByte(Vector3 v)
        {
            this.X = (sbyte)v.X;
            this.Y = (sbyte)v.Y;
            this.Z = (sbyte)v.Z;
        }

        public static Vector3SByte One
        {
            get { return new Vector3SByte(1, 1, 1); }
        }

        public static Vector3SByte Zero
        {
            get { return new Vector3SByte(0, 0, 0); }
        }

        public static Vector3SByte Right
        {
            get { return new Vector3SByte(1, 0, 0); }
        }

        public static Vector3SByte Up
        {
            get { return new Vector3SByte(0, 1, 0); }
        }

        public static Vector3SByte Forward
        {
            get { return new Vector3SByte(0, 0, 1); }
        }

        public static Vector3SByte CreateFrom(Vector3 v)
        {
            return new Vector3SByte((sbyte)v.X, (sbyte)v.Y, (sbyte)v.Z);
        }

        public static Vector3SByte operator +(Vector3SByte v1, Vector3SByte v2)
        {
            Vector3SByte result = new Vector3SByte();

            result.X = AddWithClamp(v1.X, v2.X);
            result.Y = AddWithClamp(v1.Y, v2.Y);
            result.Z = AddWithClamp(v1.Z, v2.Z);

            return result;
        }

        public static Vector3SByte operator -(Vector3SByte v1, Vector3SByte v2)
        {
            Vector3SByte result = new Vector3SByte();

            result.X = SubtractWithClamp(v1.X, v2.X);
            result.Y = SubtractWithClamp(v1.Y, v2.Y);
            result.Z = SubtractWithClamp(v1.Z, v2.Z);

            return result;
        }

        private static sbyte AddWithClamp(sbyte a, sbyte b)
        {
            int sum = a + b;
            if (sum > sbyte.MaxValue) return sbyte.MaxValue;
            if (sum < sbyte.MinValue) return sbyte.MinValue;
            return (sbyte)sum;
        }

        private static sbyte SubtractWithClamp(sbyte a, sbyte b)
        {
            int diff = a - b;
            if (diff > sbyte.MaxValue) return sbyte.MaxValue;
            if (diff < sbyte.MinValue) return sbyte.MinValue;
            return (sbyte)diff;
        }

        public static bool operator ==(Vector3SByte left, Vector3SByte right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        public static bool operator !=(Vector3SByte left, Vector3SByte right)
        {
            return !(left == right);
        }

        public static explicit operator Vector3i(Vector3SByte v)
        {
            return new Vector3i(v.X, v.Y, v.Z);
        }

        public static explicit operator Vector3SByte(Vector3i v)
        {
            return new Vector3SByte(v);
        }

        public static explicit operator Vector3(Vector3SByte v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static explicit operator Vector3SByte(Vector3 v)
        {
            return new Vector3SByte(v);
        }

        public static double Distance(Vector3SByte a, Vector3SByte b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            int dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static int DistanceSquared(Vector3SByte a, Vector3SByte b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            int dz = a.Z - b.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3SByte other)
            {
                return this == other;
            }
            return false;
        }

        public bool Equals(Vector3SByte other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }

}
