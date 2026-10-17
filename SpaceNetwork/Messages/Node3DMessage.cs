using System.Numerics;
using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class Node3DMessage : BaseMessage
    {
        public Vector3 Position;
        public Quaternion Rotation;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.WriteObject(Position);
            msg.WriteObject(Rotation);
        }

        public override void Read(NetIncomingMessage msg)
        {

            Position = msg.ReadObject<Vector3>();
            Rotation = msg.ReadObject<Quaternion>();
        }
    }
}
