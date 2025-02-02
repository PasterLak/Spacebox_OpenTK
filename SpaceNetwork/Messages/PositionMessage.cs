using System.Numerics;
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class PositionMessage : BaseMessage
    {
        public Vector3 Position;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Position.X);
            msg.Write(Position.Y);
            msg.Write(Position.Z);
        }

        public override void Read(NetIncomingMessage msg)
        {
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            Position = new Vector3(x, y,z);
        }
    }
}
