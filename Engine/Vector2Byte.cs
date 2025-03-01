using OpenTK.Mathematics;
using System.Text.Json.Serialization;

public struct Vector2Byte
{
    [JsonPropertyName("x")]
    public byte X { get; set; }

    [JsonPropertyName("y")]
    public byte Y { get; set; }

    public Vector2Byte(byte x, byte y)
    {
        this.X = x;
        this.Y = y;
    }
    public Vector2Byte(int x, int y)
    {
        this.X = (byte)x;
        this.Y = (byte)y;
    }

    public Vector2Byte(Vector2i v)
    {
        this.X = (byte)v.X;
        this.Y = (byte)v.Y;
    }

    public Vector2Byte(Vector2 v)
    {
        this.X = (byte)v.X;
        this.Y = (byte)v.Y;
    }

    public static Vector2Byte Zero
    {
        get { return new Vector2Byte(0, 0); }
    }
    public static Vector2Byte One
    {
        get { return new Vector2Byte(1, 1); }
    }

    public static Vector2Byte Right
    {
        get { return new Vector2Byte(1, 0); }
    }

    
    public static Vector2Byte Up
    {
        get { return new Vector2Byte(0, 1); }
    }

    public static bool operator ==(Vector2Byte v1, Vector2Byte v2)
    {
       
        if (v1.X != v2.X) return false;
        if (v1.Y != v2.Y) return false;

        return true;
    }
    public static bool operator !=(Vector2Byte v1, Vector2Byte v2)
    {
        return !(v1 == v2);
    }

    public static Vector2Byte operator +(Vector2Byte v1, Vector2Byte v2)
    {
        Vector2Byte result = new Vector2Byte();

        if (v1.X + v2.X <= byte.MaxValue) result.X = (byte)(v1.X + v2.X);
        else result.X = byte.MaxValue;

        if (v1.Y + v2.Y <= byte.MaxValue) result.Y = (byte)(v1.Y + v2.Y);
        else result.Y = byte.MaxValue;

        return result;
    }

    public static Vector2Byte operator -(Vector2Byte v1, Vector2Byte v2)
    {
        Vector2Byte result = new Vector2Byte();

        if (v1.X - v2.X >= 0) result.X = (byte)(v1.X - v2.X);
        else result.X = 0;

        if (v1.Y - v2.Y >= 0) result.Y = (byte)(v1.Y - v2.Y);
        else result.Y = 0;

        return result;
    }

    public static explicit operator Vector2i(Vector2Byte v)
    {
        return new Vector2i(v.X, v.Y);
    }

    public static explicit operator Vector2Byte(Vector2i v)
    {
        return new Vector2Byte(v);
    }

    public static explicit operator Vector2(Vector2Byte v)
    {
        return new Vector2(v.X, v.Y);
    }

    public static explicit operator Vector2Byte(Vector2 v)
    {
        return new Vector2Byte(v);
    }

    /// <summary>
    /// Distance between 2 vectors
    /// </summary>
    /// <param name="a">Vector a</param>
    /// <param name="b">Vector b</param>
    /// <returns>Distance between</returns>
    public static ushort Distance(Vector2Byte a, Vector2Byte b)
    {
        return (ushort)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public static ushort DistanceSquared(Vector2Byte a, Vector2Byte b)
    {
        return (ushort)((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", X, Y);
    }
}
