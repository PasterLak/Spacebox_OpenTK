
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class SeedMessage : BaseMessage
    {
        private int seed;

        public SeedMessage() { }

        public SeedMessage(int seed)
        {
            this.seed = seed;
        }

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(seed);
        }

        public override void Read(NetIncomingMessage msg)
        {

            seed = msg.ReadInt32();

        }
    }
}
