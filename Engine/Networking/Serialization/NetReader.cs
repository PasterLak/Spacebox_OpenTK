using System.Text;
using OpenTK.Mathematics;

namespace Engine.Networking;

public sealed class NetReader
{
    private byte[] _buffer = Array.Empty<byte>();
    private int _position;
    private int _end;

    public NetReader()
    {
    }

    public NetReader(byte[] data)
    {
        Set(data, 0, data.Length);
    }

    public int Remaining => _end - _position;
    public bool EndOfData => _position >= _end;

    public void Set(byte[] data, int offset, int length)
    {
        _buffer = data;
        _position = offset;
        _end = offset + length;
    }

    public byte ReadByte()
    {
        Require(1);
        return _buffer[_position++];
    }

    public sbyte ReadSByte() => (sbyte)ReadByte();
    public bool ReadBool() => ReadByte() != 0;

    public short ReadShort()
    {
        Require(2);
        var value = BitConverter.ToInt16(_buffer, _position);
        _position += 2;
        return value;
    }

    public ushort ReadUShort()
    {
        Require(2);
        var value = BitConverter.ToUInt16(_buffer, _position);
        _position += 2;
        return value;
    }

    public int ReadInt()
    {
        Require(4);
        var value = BitConverter.ToInt32(_buffer, _position);
        _position += 4;
        return value;
    }

    public uint ReadUInt()
    {
        Require(4);
        var value = BitConverter.ToUInt32(_buffer, _position);
        _position += 4;
        return value;
    }

    public long ReadLong()
    {
        Require(8);
        var value = BitConverter.ToInt64(_buffer, _position);
        _position += 8;
        return value;
    }

    public ulong ReadULong()
    {
        Require(8);
        var value = BitConverter.ToUInt64(_buffer, _position);
        _position += 8;
        return value;
    }

    public float ReadFloat()
    {
        Require(4);
        var value = BitConverter.ToSingle(_buffer, _position);
        _position += 4;
        return value;
    }

    public double ReadDouble()
    {
        Require(8);
        var value = BitConverter.ToDouble(_buffer, _position);
        _position += 8;
        return value;
    }

    public string ReadString()
    {
        int length = ReadInt();
        if (length < 0) return null;

        Require(length);
        var value = Encoding.UTF8.GetString(_buffer, _position, length);
        _position += length;
        return value;
    }

    public Vector2 ReadVector2() => new Vector2(ReadFloat(), ReadFloat());
    public Vector3 ReadVector3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
    public Vector3i ReadVector3i() => new Vector3i(ReadInt(), ReadInt(), ReadInt());
    public Quaternion ReadQuaternion() => new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());

    public byte[] ReadBytes()
    {
        int length = ReadInt();
        Require(length);
        var data = new byte[length];
        System.Buffer.BlockCopy(_buffer, _position, data, 0, length);
        _position += length;
        return data;
    }

    public ReadOnlySpan<byte> ReadRemaining()
    {
        var span = new ReadOnlySpan<byte>(_buffer, _position, _end - _position);
        _position = _end;
        return span;
    }

    private void Require(int count)
    {
        if (count < 0 || _position + count > _end)
            throw new InvalidOperationException($"[NetReader] Tried to read {count} bytes with {_end - _position} remaining");
    }
}
