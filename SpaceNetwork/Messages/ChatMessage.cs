using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class ChatMessage : BaseMessage
    {
        public int SenderId;
        public string SenderName;
        public string Text;

        public ChatMessage() { }
        public ChatMessage(int senderId, string name, string text) 
        {
            SenderId = senderId;
            SenderName = name;
            Text = text;
        }

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(SenderId);
            msg.Write(SenderName);
            msg.Write(Text);
        }

        public override void Read(NetIncomingMessage msg)
        {
            SenderId = msg.ReadInt32();
            SenderName = msg.ReadString();
            Text = msg.ReadString();
        }
    }
}
