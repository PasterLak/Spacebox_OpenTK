using System.Numerics;
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class RotationMessage : BaseMessage
    {
        public Vector3 Rotation;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Rotation.X);
            msg.Write(Rotation.Y);
            msg.Write(Rotation.Z);
        }

        public override void Read(NetIncomingMessage msg)
        {
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            Rotation = new Vector3(x, y, z);
        }
    }
}
