
using Lidgren.Network;
using SpaceNetwork.Messages;

namespace SpaceNetwork
{
    public static class MessageFactory
    {
        public static BaseMessage CreateMessage(NetIncomingMessage msg)
        {
            int id = msg.ReadInt32();
            var m = MessageRegistry.CreateMessage(id);
            m.Read(msg);
            return m;
        }
    }
}
