using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class BlockDestroyedMessage : BaseMessage
    {
        public int senderID;
        public int X;
        public int Y;
        public int Z;
        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(senderID);
            msg.Write(X);
            msg.Write(Y);
            msg.Write(Z);
        }
        public override void Read(NetIncomingMessage msg)
        {
            senderID = msg.ReadInt32();
            X = msg.ReadInt32();
            Y = msg.ReadInt32();
            Z = msg.ReadInt32();
        }
    }
}