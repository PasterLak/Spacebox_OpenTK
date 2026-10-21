using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public static class BlockVertex
{
    public const int Words = 7;

    public static void Write(float[] destination, ref int offset, Vector3 position, Vector2 uv, Vector3 color, Vector3 normal, float ao, bool emission)
    {
        destination[offset++] = position.X;
        destination[offset++] = position.Y;
        destination[offset++] = position.Z;
        destination[offset++] = uv.X;
        destination[offset++] = uv.Y;
        destination[offset++] = BitConverter.Int32BitsToSingle((int)PackColor(color, emission));
        destination[offset++] = BitConverter.Int32BitsToSingle((int)PackNormal(normal, ao));
    }

    public static void Append(List<float> destination, Vector3 position, Vector2 uv, Vector3 color, Vector3 normal, float ao, bool emission)
    {
        destination.Add(position.X);
        destination.Add(position.Y);
        destination.Add(position.Z);
        destination.Add(uv.X);
        destination.Add(uv.Y);
        destination.Add(BitConverter.Int32BitsToSingle((int)PackColor(color, emission)));
        destination.Add(BitConverter.Int32BitsToSingle((int)PackNormal(normal, ao)));
    }

    public static uint PackColor(Vector3 color, bool emission)
    {
        return Pack(ToByte(color.X), ToByte(color.Y), ToByte(color.Z), emission ? (byte)255 : (byte)0);
    }

    public static uint PackNormal(Vector3 normal, float ao)
    {
        return Pack(ToByte(normal.X * 0.5f + 0.5f), ToByte(normal.Y * 0.5f + 0.5f), ToByte(normal.Z * 0.5f + 0.5f), ToByte(ao));
    }

    public static Vector4 Unpack(uint packed)
    {
        return new Vector4(packed & 0xFF, (packed >> 8) & 0xFF, (packed >> 16) & 0xFF, packed >> 24) / 255f;
    }

    private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);

    private static uint Pack(byte x, byte y, byte z, byte w) => x | ((uint)y << 8) | ((uint)z << 16) | ((uint)w << 24);
}
