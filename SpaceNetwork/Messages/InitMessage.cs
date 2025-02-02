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
            msg.Write(Player.Position.X);
            msg.Write(Player.Position.Y);
            msg.Write(Player.Position.Z);
            msg.Write(Player.Rotation.X);
            msg.Write(Player.Rotation.Y);
            msg.Write(Player.Rotation.Z);
            msg.Write(Player.Rotation.W);
            msg.Write(ColorHelper.VectorToHex(Player.Color));
        }

        public override void Read(NetIncomingMessage msg)
        {
            Player = new Player();
            Player.ID = msg.ReadInt32();
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            float xr = msg.ReadFloat();
            float yr = msg.ReadFloat();
            float zr = msg.ReadFloat();
            float w = msg.ReadFloat();
            var hex = msg.ReadString();
            Player.Position = new Vector3(x, y,z);
            Player.Rotation = new Vector4(xr, yr, zr,w);
            Player.Color = ColorHelper.HexToVector(hex);
            Player.DisplayedPosition = Player.Position;
        }
    }
}
