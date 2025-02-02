using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class KickMessage : BaseMessage
    {
        public string Reason;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Reason);
        }

        public override void Read(NetIncomingMessage msg)
        {
            Reason = msg.ReadString();
        }
    }
}
