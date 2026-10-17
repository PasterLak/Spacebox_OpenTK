using Engine.Networking;

namespace Spacebox.Network.Messages;

public struct ChatSendMessage : INetMessage
{
    public string Text;

    public void Serialize(NetWriter writer) => writer.Write(Text);
    public void Deserialize(NetReader reader) => Text = reader.ReadString();
}

public struct ChatBroadcastMessage : INetMessage
{
    public string Text;
    public uint Color;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Text);
        writer.Write(Color);
    }

    public void Deserialize(NetReader reader)
    {
        Text = reader.ReadString();
        Color = reader.ReadUInt();
    }
}
