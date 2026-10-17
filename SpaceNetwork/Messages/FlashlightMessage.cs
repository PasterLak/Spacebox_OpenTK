using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class FlashlightMessage : BaseMessage
    {
        public int SenderId;
        public bool State;

        public FlashlightMessage() { }
        public FlashlightMessage(int senderId, bool state)
        {
            SenderId = senderId;
            State =  state;
        }

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(SenderId);
            msg.Write(State);
          
        }

        public override void Read(NetIncomingMessage msg)
        {
            SenderId = msg.ReadInt32();
            State = msg.ReadBoolean();
            
        }
    }
}
