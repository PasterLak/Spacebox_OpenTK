using System.Text;
using OpenTK.Mathematics;

namespace Engine.Networking;

public sealed class NetWriter
{
    private byte[] _buffer;
    private int _length;

    public NetWriter(int capacity = 256)
    {
        _buffer = new byte[Math.Max(16, capacity)];
    }

    public int Length => _length;
    public byte[] Buffer => _buffer;
    public ReadOnlySpan<byte> Span => new ReadOnlySpan<byte>(_buffer, 0, _length);

    public void Reset()
    {
        _length = 0;
    }

    public byte[] ToArray()
    {
        var copy = new byte[_length];
        System.Buffer.BlockCopy(_buffer, 0, copy, 0, _length);
        return copy;
    }

    public void Write(byte value)
    {
        Ensure(1);
        _buffer[_length++] = value;
    }

    public void Write(sbyte value) => Write((byte)value);
    public void Write(bool value) => Write(value ? (byte)1 : (byte)0);

    public void Write(short value)
    {
        Ensure(2);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 2), value);
        _length += 2;
    }

    public void Write(ushort value)
    {
        Ensure(2);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 2), value);
        _length += 2;
    }

    public void Write(int value)
    {
        Ensure(4);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 4), value);
        _length += 4;
    }

    public void Write(uint value)
    {
        Ensure(4);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 4), value);
        _length += 4;
    }

    public void Write(long value)
    {
        Ensure(8);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 8), value);
        _length += 8;
    }

    public void Write(ulong value)
    {
        Ensure(8);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 8), value);
        _length += 8;
    }

    public void Write(float value)
    {
        Ensure(4);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 4), value);
        _length += 4;
    }

    public void Write(double value)
    {
        Ensure(8);
        BitConverter.TryWriteBytes(new Span<byte>(_buffer, _length, 8), value);
        _length += 8;
    }

    public void Write(string value)
    {
        if (value == null)
        {
            Write(-1);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        Write(byteCount);
        Ensure(byteCount);
        Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _length);
        _length += byteCount;
    }

    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    public void Write(Vector3i value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    public void Write(Quaternion value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
        Write(value.W);
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        Write(data.Length);
        WriteRaw(data);
    }

    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        Ensure(data.Length);
        data.CopyTo(new Span<byte>(_buffer, _length, data.Length));
        _length += data.Length;
    }

    private void Ensure(int count)
    {
        int required = _length + count;
        if (required <= _buffer.Length) return;

        int size = _buffer.Length * 2;
        while (size < required) size *= 2;
        Array.Resize(ref _buffer, size);
    }
}
