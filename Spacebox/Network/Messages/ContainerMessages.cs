using Engine.Networking;
using OpenTK.Mathematics;

namespace Spacebox.Network.Messages;

public readonly struct ContainerKey : IEquatable<ContainerKey>
{
    public readonly long EntityId;
    public readonly Vector3i Cell;
    public readonly bool IsObject;

    public ContainerKey(long entityId, Vector3i cell) : this(entityId, cell, false)
    {
    }

    private ContainerKey(long entityId, Vector3i cell, bool isObject)
    {
        EntityId = entityId;
        Cell = cell;
        IsObject = isObject;
    }

    public static ContainerKey ForObject(uint netId) => new ContainerKey(netId, Vector3i.Zero, true);

    public void Write(NetWriter writer)
    {
        writer.Write(IsObject);
        writer.Write(EntityId);
        writer.Write(Cell);
    }

    public static ContainerKey Read(NetReader reader)
    {
        bool isObject = reader.ReadBool();
        return new ContainerKey(reader.ReadLong(), reader.ReadVector3i(), isObject);
    }

    public bool Equals(ContainerKey other) => IsObject == other.IsObject && EntityId == other.EntityId && Cell == other.Cell;
    public override bool Equals(object obj) => obj is ContainerKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(IsObject, EntityId, Cell);
}

public struct SlotState : IEquatable<SlotState>
{
    public short ItemId;
    public byte Count;

    public static readonly SlotState Empty = new SlotState { ItemId = -1, Count = 0 };

    public bool IsEmpty => ItemId < 0 || Count == 0;

    public bool Equals(SlotState other) => (IsEmpty && other.IsEmpty) || (ItemId == other.ItemId && Count == other.Count);
    public override bool Equals(object obj) => obj is SlotState other && Equals(other);
    public override int GetHashCode() => IsEmpty ? 0 : HashCode.Combine(ItemId, Count);

    public static void WriteArray(NetWriter writer, SlotState[] slots)
    {
        writer.Write((ushort)slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            writer.Write(slots[i].IsEmpty ? (short)-1 : slots[i].ItemId);
            writer.Write(slots[i].IsEmpty ? (byte)0 : slots[i].Count);
        }
    }

    public static SlotState[] ReadArray(NetReader reader)
    {
        var slots = new SlotState[reader.ReadUShort()];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = new SlotState { ItemId = reader.ReadShort(), Count = reader.ReadByte() };
        return slots;
    }
}

public struct ItemAmount
{
    public short ItemId;
    public int Amount;

    public static void WriteList(NetWriter writer, List<ItemAmount> amounts)
    {
        writer.Write((ushort)amounts.Count);
        for (int i = 0; i < amounts.Count; i++)
        {
            writer.Write(amounts[i].ItemId);
            writer.Write(amounts[i].Amount);
        }
    }

    public static List<ItemAmount> ReadList(NetReader reader)
    {
        int count = reader.ReadUShort();
        var amounts = new List<ItemAmount>(count);
        for (int i = 0; i < count; i++)
            amounts.Add(new ItemAmount { ItemId = reader.ReadShort(), Amount = reader.ReadInt() });
        return amounts;
    }
}

public struct ContainerOpenMessage : INetMessage
{
    public ContainerKey Key;

    public void Serialize(NetWriter writer) => Key.Write(writer);
    public void Deserialize(NetReader reader) => Key = ContainerKey.Read(reader);
}

public struct ContainerCloseMessage : INetMessage
{
    public ContainerKey Key;

    public void Serialize(NetWriter writer) => Key.Write(writer);
    public void Deserialize(NetReader reader) => Key = ContainerKey.Read(reader);
}

public struct ContainerSnapshotMessage : INetMessage
{
    public ContainerKey Key;
    public int Version;
    public SlotState[] Slots;
    public byte[] State;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(Version);
        SlotState.WriteArray(writer, Slots);
        writer.WriteBytes(State ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Version = reader.ReadInt();
        Slots = SlotState.ReadArray(reader);
        State = reader.ReadBytes();
    }
}

public struct ContainerChangeMessage : INetMessage
{
    public ContainerKey Key;
    public int BaseVersion;
    public SlotState[] Desired;
    public List<ItemAmount> Deltas;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(BaseVersion);
        SlotState.WriteArray(writer, Desired);
        ItemAmount.WriteList(writer, Deltas);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        BaseVersion = reader.ReadInt();
        Desired = SlotState.ReadArray(reader);
        Deltas = ItemAmount.ReadList(reader);
    }
}

public struct ContainerChangeResultMessage : INetMessage
{
    public ContainerKey Key;
    public int NewVersion;
    public List<ItemAmount> DeniedRemovals;
    public List<ItemAmount> RejectedAdditions;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(NewVersion);
        ItemAmount.WriteList(writer, DeniedRemovals);
        ItemAmount.WriteList(writer, RejectedAdditions);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        NewVersion = reader.ReadInt();
        DeniedRemovals = ItemAmount.ReadList(reader);
        RejectedAdditions = ItemAmount.ReadList(reader);
    }
}
