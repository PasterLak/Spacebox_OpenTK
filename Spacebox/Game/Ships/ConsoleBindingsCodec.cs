using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public static class ConsoleBindingsCodec
{
    public static void Write(NetWriter writer, ConsoleBindings bindings)
    {
        writer.Write((byte)bindings.GroupCount);
        writer.Write(bindings.Weapons);
        for (int i = 0; i < bindings.GroupCount; i++)
            writer.Write(bindings.GetKey(i));

        writer.Write(bindings.Camera.HasValue);
        if (bindings.Camera.HasValue) writer.Write(bindings.Camera.Value);

        writer.Write((ushort)bindings.Bound.Count);
        foreach (var pair in bindings.Bound)
        {
            writer.Write(pair.Key);
            writer.Write(pair.Value);
        }
    }

    public static ConsoleBindings Read(NetReader reader)
    {
        int groupCount = reader.ReadByte();
        bool weapons = reader.ReadBool();
        var bindings = new ConsoleBindings(groupCount, weapons);

        for (int i = 0; i < groupCount; i++)
        {
            int key = reader.ReadInt();
            bindings.SetKey(i, ShipKeys.IsValid(key) ? key : ShipKeys.None);
        }

        if (reader.ReadBool()) bindings.SetCamera(reader.ReadVector3i());

        int count = reader.ReadUShort();
        for (int i = 0; i < count; i++)
        {
            var cell = reader.ReadVector3i();
            bindings.Bind(cell, reader.ReadByte());
        }

        return bindings;
    }

    public static byte[] ToBytes(ConsoleBindings bindings)
    {
        var writer = new NetWriter(64);
        Write(writer, bindings);
        return writer.ToArray();
    }

    public static ConsoleBindings FromBytes(byte[] data)
    {
        return data == null || data.Length == 0 ? null : Read(new NetReader(data));
    }
}
