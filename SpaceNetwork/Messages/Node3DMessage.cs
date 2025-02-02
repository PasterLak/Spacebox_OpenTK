using System.Numerics;
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class Node3DMessage : BaseMessage
    {
        public Vector3 Position;
        public Vector4 Rotation;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Position.X);
            msg.Write(Position.Y);
            msg.Write(Position.Z);
            msg.Write(Rotation.X);
            msg.Write(Rotation.Y);
            msg.Write(Rotation.Z);
            msg.Write(Rotation.W);
        }

        public override void Read(NetIncomingMessage msg)
        {
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            Position = new Vector3(x, y,z);

            float x1 = msg.ReadFloat();
            float y1 = msg.ReadFloat();
            float z1 = msg.ReadFloat();
            float w1 = msg.ReadFloat();
            Rotation = new Vector4(x1, y1, z1,w1);
        }
    }
}
