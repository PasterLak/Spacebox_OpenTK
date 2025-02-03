
using Lidgren.Network;
namespace SpaceNetwork.Messages
{
    public class BlockPlaceMessage : BaseMessage
    {
        public int senderID;
        public short blockID;
        public long packedData;
        public BlockPlaceMessage() { }
        public BlockPlaceMessage(int senderID, short blockID, byte direction, short x, short y, short z)
        {
            this.senderID = senderID;
            this.blockID = blockID;
            packedData = ((long)direction << 48) |
                         ((long)(ushort)x << 32) |
                         ((long)(ushort)y << 16) |
                         ((long)(ushort)z);
        }
        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(senderID);
            msg.Write(blockID);
            msg.Write(packedData);
        }
        public override void Read(NetIncomingMessage msg)
        {
            senderID = msg.ReadInt32();
            blockID = msg.ReadInt16();
            packedData = msg.ReadInt64();
        }
        public byte GetDirection() { return (byte)(packedData >> 48); }
        public short GetX() { return (short)((packedData >> 32) & 0xFFFF); }
        public short GetY() { return (short)((packedData >> 16) & 0xFFFF); }
        public short GetZ() { return (short)(packedData & 0xFFFF); }
    }
}
