using OpenTK.Mathematics;
using System;
using System.Text.Json.Serialization;


namespace Engine
{
    
    public struct Color3Byte : IEquatable<Color3Byte>
    {
        [JsonPropertyName("r")] public byte R { get; set; }
        [JsonPropertyName("g")] public byte G { get; set; }
        [JsonPropertyName("b")] public byte B { get; set; }

        public Color3Byte(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public Color3Byte(Vector3Byte rgb)
        {
            this.R = rgb.X;
            this.G = rgb.Y;
            this.B = rgb.Z;
        }

        public Color3Byte(int r, int g, int b)
        {
            this.R = (byte)Clamp(r, 0, byte.MaxValue);
            this.G = (byte)Clamp(g, 0, byte.MaxValue);
            this.B = (byte)Clamp(b, 0, byte.MaxValue);
        }

        public Color3Byte(Vector3i v)
        {
            this.R = (byte)Clamp(v.X, 0, byte.MaxValue);
            this.G = (byte)Clamp(v.Y, 0, byte.MaxValue);
            this.B = (byte)Clamp(v.Z, 0, byte.MaxValue);
        }

        public Color3Byte(Vector3 v)
        {
            this.R = (byte)Clamp((int)v.X, 0, byte.MaxValue);
            this.G = (byte)Clamp((int)v.Y, 0, byte.MaxValue);
            this.B = (byte)Clamp((int)v.Z, 0, byte.MaxValue);
        }

        public static Color3Byte White => new Color3Byte(255, 255, 255);
        public static Color3Byte Black => new Color3Byte(0, 0, 0);
        public static Color3Byte Red => new Color3Byte(255, 0, 0);
        public static Color3Byte Green => new Color3Byte(0, 255, 0);
        public static Color3Byte Blue => new Color3Byte(0, 0, 255);
        public static Color3Byte Yellow => new Color3Byte(255, 255, 0);
        public static Color3Byte Cyan => new Color3Byte(0, 255, 255);
        public static Color3Byte Magenta => new Color3Byte(255, 0, 255);
        public static Color3Byte Orange => new Color3Byte(255, 165, 0);
        public static Color3Byte Purple => new Color3Byte(128, 0, 128);
        public static Color3Byte Gray => new Color3Byte(128, 128, 128);
        public static Color3Byte Transparent => new Color3Byte(0, 0, 0);

        public static Color3Byte operator +(Color3Byte c1, Color3Byte c2)
        {
            return new Color3Byte(
                (byte)Math.Min(c1.R + c2.R, byte.MaxValue),
                (byte)Math.Min(c1.G + c2.G, byte.MaxValue),
                (byte)Math.Min(c1.B + c2.B, byte.MaxValue)
            );
        }

        public static Color3Byte operator -(Color3Byte c1, Color3Byte c2)
        {
            return new Color3Byte(
                (byte)Math.Max(c1.R - c2.R, 0),
                (byte)Math.Max(c1.G - c2.G, 0),
                (byte)Math.Max(c1.B - c2.B, 0)
            );
        }

        public static bool operator ==(Color3Byte left, Color3Byte right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color3Byte left, Color3Byte right)
        {
            return !(left == right);
        }

        public float[] ToFloatArray()
        {
            return new float[] { R / 255f, G / 255f, B / 255f };
        }

        public Color4 ToColor4()
        {
            return new Color4(R / 255f, G / 255f, B / 255f, 1.0f);
        }

        public Color4 ToColor4(float alpha)
        {
            return new Color4(R / 255f, G / 255f, B / 255f, alpha);
        }

        public uint ToUInt()
        {
            return ((uint)R) | ((uint)G << 8) | ((uint)B << 16) | (255u << 24);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(R / 255f, G / 255f, B / 255f);
        }

        public Vector4 ToVector4()
        {
            return new Vector4(R / 255f, G / 255f, B / 255f, 1.0f);
        }
        public Vector4 ToVector4(float alpha)
        {
            return new Vector4(R / 255f, G / 255f, B / 255f, alpha);
        }

        public int ToInt()
        {
            return R | (G << 8) | (B << 16) | (255 << 24);
        }

        public static Color3Byte FromInt(int packed)
        {
            byte r = (byte)(packed & 0xFF);
            byte g = (byte)((packed >> 8) & 0xFF);
            byte b = (byte)((packed >> 16) & 0xFF);
            return new Color3Byte(r, g, b);
        }

        private static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public bool Equals(Color3Byte other)
        {
            return this.R == other.R && this.G == other.G && this.B == other.B;
        }

        public override bool Equals(object obj)
        {
            if (obj is Color3Byte)
            {
                return Equals((Color3Byte)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        public override string ToString()
        {
            return $"(R:{R}, G:{G}, B:{B})";
        }
    }

}
