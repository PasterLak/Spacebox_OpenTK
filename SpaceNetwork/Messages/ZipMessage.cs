using Lidgren.Network;


namespace SpaceNetwork.Messages
{
    public class ZipMessage : BaseMessage
    {
        public string ModName { get; set; }
        public byte[] ZipData { get; set; }
        protected override void WriteData(NetOutgoingMessage msg)

        {
            msg.Write(ModName);
            msg.Write(ZipData.Length);
            msg.Write(ZipData);
        }
        public override void Read(NetIncomingMessage msg)
        {
            ModName = msg.ReadString();
            int length = msg.ReadInt32();
            ZipData = msg.ReadBytes(length);
        }
    }
}
