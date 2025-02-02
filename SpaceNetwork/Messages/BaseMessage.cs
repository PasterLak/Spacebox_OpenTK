using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public abstract class BaseMessage
    {
        public void Write(NetOutgoingMessage msg)
        {
            int id = MessageRegistry.GetId(this.GetType());
            msg.Write(id);
            WriteData(msg);
        }

        protected abstract void WriteData(NetOutgoingMessage msg);
        public abstract void Read(NetIncomingMessage msg);
    }
}
