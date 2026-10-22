using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct InventoryCorrectionMessage : INetMessage
{
    public int Seq;
    public List<ItemAmount> Remove;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Seq);
        ItemAmount.WriteList(writer, Remove ?? new List<ItemAmount>());
    }

    public void Deserialize(NetReader reader)
    {
        Seq = reader.ReadInt();
        Remove = ItemAmount.ReadList(reader);
    }
}

public struct CraftClaimMessage : INetMessage
{
    public short ProductItemId;
    public byte Times;

    public void Serialize(NetWriter writer)
    {
        writer.Write(ProductItemId);
        writer.Write(Times);
    }

    public void Deserialize(NetReader reader)
    {
        ProductItemId = reader.ReadShort();
        Times = reader.ReadByte();
    }
}

public struct TutorialRewardClaimMessage : INetMessage
{
    public string TutorialId;
    public string RewardKey;

    public void Serialize(NetWriter writer)
    {
        writer.Write(TutorialId ?? string.Empty);
        writer.Write(RewardKey ?? string.Empty);
    }

    public void Deserialize(NetReader reader)
    {
        TutorialId = reader.ReadString();
        RewardKey = reader.ReadString();
    }
}
