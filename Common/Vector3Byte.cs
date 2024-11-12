using OpenTK.Mathematics;
using System.Text.Json.Serialization;

public struct Vector3Byte
{
    [JsonPropertyName("x")]
    public byte X { get; set; }
    [JsonPropertyName("y")]
    public byte Y { get; set; }
    [JsonPropertyName("z")]
    public byte Z { get; set; }

    public Vector3Byte(byte x, byte y, byte z)
	{
		this.X = x;
		this.Y = y;
		this.Z = z;
	}
	public Vector3Byte(Vector3i v)
	{
		this.X = (byte)v.X;
		this.Y = (byte)v.Y;
		this.Z = (byte)v.Z;
	}
	public Vector3Byte(Vector3 v)
	{
		this.X = (byte)v.X;
		this.Y = (byte)v.Y;
		this.Z = (byte)v.Z;
	}

    public static Vector3Byte One
    {
        get { return new Vector3Byte(1,1,1); }
    }
    public static Vector3Byte Zero
	{
		get { return new Vector3Byte(0, 0, 0); }
	}

	public static Vector3Byte Right
	{
		get { return new Vector3Byte(1, 0, 0); }
	}
	public static Vector3Byte Forward
	{
		get { return new Vector3Byte(0, 0, 1); }
	}

	
	public static Vector3Byte Up
	{
		get { return new Vector3Byte(0, 1, 0); }
	}

	public static Vector3Byte operator + (Vector3Byte v1, Vector3Byte v2)
	{
		Vector3Byte result = new Vector3Byte();

		if (v1.X + v2.X <= Byte.MaxValue) result.X = (byte)(v1.X + v2.X);
		else result.X = Byte.MaxValue;

		if (v1.Y + v2.Y <= Byte.MaxValue) result.Y = (byte)(v1.Y + v2.Y);
		else result.Y = Byte.MaxValue;

		if (v1.Z + v2.Z <= Byte.MaxValue) result.Z = (byte)(v1.Z + v2.Z);
		else result.Z = Byte.MaxValue;

		return result;
	}

	public static Vector3Byte operator -(Vector3Byte v1, Vector3Byte v2)
	{
		Vector3Byte result = new Vector3Byte();

		if (v1.X - v2.X >= 0) result.X = (byte)(v1.X - v2.X);
		else result.X = 0;

		if (v1.Y - v2.Y >= 0) result.Y = (byte)(v1.Y - v2.Y);
		else result.Y = 0;

		if (v1.Z - v2.Z >= 0) result.Z = (byte)(v1.Z - v2.Z);
		else result.Z = 0;


		return result;
	}
    public static bool operator ==(Vector3Byte left, Vector3Byte right)
    {
        return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
    }

    public static bool operator !=(Vector3Byte left, Vector3Byte right)
    {
        return !(left == right);
    }


    public static explicit operator Vector3i(Vector3Byte v)
	{
		return new Vector3i(v.X, v.Y, v.Z);
	}
	public static explicit operator Vector3Byte(Vector3i v)
	{
		return new Vector3Byte(v);
	}

	public static explicit operator Vector3(Vector3Byte v)
	{
		return new Vector3(v.X, v.Y, v.Z);
	}
	public static explicit operator Vector3Byte(Vector3 v)
	{
		return new Vector3Byte(v);
	}

	public static ushort Distance(Vector3Byte a, Vector3Byte b)
	{
		return (ushort)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
	}
	
	public static ushort DistanceSquared(Vector3Byte a, Vector3Byte b)
	{
		return (ushort)((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));
	}



	public override string ToString()
	{
		
		return string.Format("({0},{1},{2})", X, Y, Z);
	}


}