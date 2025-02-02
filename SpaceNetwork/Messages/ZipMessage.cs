using Lidgren.Network;


namespace SpaceNetwork.Messages
{
    public class ZipMessage : BaseMessage
    {
        public byte[] ZipData { get; set; }
        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(ZipData.Length);
            msg.Write(ZipData);
        }
        public override void Read(NetIncomingMessage msg)
        {
            int length = msg.ReadInt32();
            ZipData = msg.ReadBytes(length);
        }
    }
}
