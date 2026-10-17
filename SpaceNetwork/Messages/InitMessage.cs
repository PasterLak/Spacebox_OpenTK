using System.Numerics;
using Lidgren.Network;
using SpaceNetwork.Utilities;

namespace SpaceNetwork.Messages
{
    public class InitMessage : BaseMessage
    {
        public Player Player;

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Player.ID);
            msg.WriteObject(Player.Position);
            msg.WriteObject(Player.Rotation);
            msg.Write(ColorHelper.VectorToHex(Player.Color));
        }

        public override void Read(NetIncomingMessage msg)
        {
            Player = new Player();

            Player.ID = msg.ReadInt32();
            Player.Position = msg.ReadObject<Vector3>();
            Player.Rotation = msg.ReadObject<Quaternion>();

            Player.Color = ColorHelper.HexToVector(msg.ReadString());
            Player.DisplayedPosition = Player.Position;
        }
    }
}
