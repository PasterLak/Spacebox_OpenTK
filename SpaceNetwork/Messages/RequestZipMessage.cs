using Lidgren.Network;

namespace SpaceNetwork.Messages
{

    public class RequestZipMessage : BaseMessage
    {
        protected override void WriteData(NetOutgoingMessage msg) { }
        public override void Read(NetIncomingMessage msg) { }
    }
}