using OpenTK.Mathematics;
using System.Text.Json.Serialization;

namespace Engine
{
    public struct Color4Byte : IEquatable<Color4Byte>
    {
        [JsonPropertyName("r")] public byte R { get; set; }
        [JsonPropertyName("g")] public byte G { get; set; }
        [JsonPropertyName("b")] public byte B { get; set; }
        [JsonPropertyName("a")] public byte A { get; set; }

        public Color4Byte(byte i)
        {
            this.R = i;
            this.G = i;
            this.B = i;
            this.A = 255;
        }

        public Color4Byte(byte r, byte g, byte b, byte a = 255)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        public Color4Byte(Color3Byte rgb, byte a = 255)
        {
            this.R = rgb.R;
            this.G = rgb.G;
            this.B = rgb.B;
            this.A = a;
        }

        public Color4Byte(int r, int g, int b, int a = 255)
        {
            this.R = (byte)Clamp(r, 0, byte.MaxValue);
            this.G = (byte)Clamp(g, 0, byte.MaxValue);
            this.B = (byte)Clamp(b, 0, byte.MaxValue);
            this.A = (byte)Clamp(a, 0, byte.MaxValue);
        }

        public Color4Byte(Vector4 v)
        {
            v = v * 255;
            this.R = (byte)Clamp((int)v.X, 0, byte.MaxValue);
            this.G = (byte)Clamp((int)v.Y, 0, byte.MaxValue);
            this.B = (byte)Clamp((int)v.Z, 0, byte.MaxValue);
            this.A = (byte)Clamp((int)v.W, 0, byte.MaxValue);
        }

        public Color4Byte(Color4 color)
        {
            this.R = (byte)Clamp((int)(color.R * 255), 0, byte.MaxValue);
            this.G = (byte)Clamp((int)(color.G * 255), 0, byte.MaxValue);
            this.B = (byte)Clamp((int)(color.B * 255), 0, byte.MaxValue);
            this.A = (byte)Clamp((int)(color.A * 255), 0, byte.MaxValue);
        }

        public static Color4Byte White => new Color4Byte(255, 255, 255, 255);
        public static Color4Byte Black => new Color4Byte(0, 0, 0, 255);
        public static Color4Byte Red => new Color4Byte(255, 0, 0, 255);
        public static Color4Byte Green => new Color4Byte(0, 255, 0, 255);
        public static Color4Byte Blue => new Color4Byte(0, 0, 255, 255);
        public static Color4Byte Yellow => new Color4Byte(255, 255, 0, 255);
        public static Color4Byte Cyan => new Color4Byte(0, 255, 255, 255);
        public static Color4Byte Magenta => new Color4Byte(255, 0, 255, 255);
        public static Color4Byte Transparent => new Color4Byte(0, 0, 0, 0);

        public static Color4Byte RandomColor(byte min, byte max, byte alpha = 255)
        {
            Random r = new Random();
            return new Color4Byte((byte)r.Next(min, max + 1), (byte)r.Next(min, max + 1), (byte)r.Next(min, max + 1), alpha);
        }

        public static bool operator ==(Color4Byte left, Color4Byte right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color4Byte left, Color4Byte right)
        {
            return !(left == right);
        }

        public Color4 ToColor4()
        {
            return new Color4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public Color3Byte ToColor3Byte()
        {
            return new Color3Byte(R, G, B);
        }

        public uint ToUInt()
        {
            return ((uint)R) | ((uint)G << 8) | ((uint)B << 16) | ((uint)A << 24);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public System.Numerics.Vector4 ToSystemVector4()
        {
            return new System.Numerics.Vector4(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public int ToInt()
        {
            return R | (G << 8) | (B << 16) | (A << 24);
        }

        public static Color4Byte FromInt(int packed)
        {
            byte r = (byte)(packed & 0xFF);
            byte g = (byte)((packed >> 8) & 0xFF);
            byte b = (byte)((packed >> 16) & 0xFF);
            byte a = (byte)((packed >> 24) & 0xFF);
            return new Color4Byte(r, g, b, a);
        }

        private static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public bool Equals(Color4Byte other)
        {
            return this.R == other.R && this.G == other.G && this.B == other.B && this.A == other.A;
        }

        public override bool Equals(object obj)
        {
            if (obj is Color4Byte)
            {
                return Equals((Color4Byte)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        public override string ToString()
        {
            return $"(R:{R}, G:{G}, B:{B}, A:{A})";
        }
    }
}