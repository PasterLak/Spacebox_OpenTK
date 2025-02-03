using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class BlockDestroyedMessage : BaseMessage
    {
        public int senderID;
        public short X;
        public short Y;
        public short Z;
        private long packedCoordinates;

        public BlockDestroyedMessage() { }

        public BlockDestroyedMessage(int senderID, short x, short y, short z)
        {
            this.senderID = senderID;
            X = x;
            Y = y;
            Z = z;
            packedCoordinates = (((long)(ushort)x) << 32) | (((long)(ushort)y) << 16) | ((long)(ushort)z);
        }

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(senderID);
            msg.Write(packedCoordinates);
        }

        public override void Read(NetIncomingMessage msg)
        {
            senderID = msg.ReadInt32();
            packedCoordinates = msg.ReadInt64();
            X = (short)((packedCoordinates >> 32) & 0xFFFF);
            Y = (short)((packedCoordinates >> 16) & 0xFFFF);
            Z = (short)(packedCoordinates & 0xFFFF);
        }
    }
}
