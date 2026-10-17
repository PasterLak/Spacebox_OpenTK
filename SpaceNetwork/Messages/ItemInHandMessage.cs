using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class ItemInHandMessage : BaseMessage
    {
        public int SenderId;
        public short ItemId;

        public ItemInHandMessage() { }
        public ItemInHandMessage(int senderId, short itemId)
        {
            SenderId = senderId;
            ItemId = itemId;
        }

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(SenderId);
            msg.Write(ItemId);
          
        }

        public override void Read(NetIncomingMessage msg)
        {
            SenderId = msg.ReadInt32();
            ItemId = msg.ReadInt16();
            
        }
    }
}
