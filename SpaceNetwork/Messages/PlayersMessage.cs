
using System.Numerics;
using Lidgren.Network;
using SpaceNetwork.Utilities;

namespace SpaceNetwork.Messages
{
    public class PlayersMessage : BaseMessage
    {
        public Dictionary<int, Player> Players = new Dictionary<int, Player>();

        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Players.Count);
            foreach (var p in Players.Values)
            {
                msg.Write(p.ID);
                msg.Write(p.Name);
                msg.Write(ColorHelper.VectorToHex(p.Color));
                msg.Write(p.Position.X);
                msg.Write(p.Position.Y);
                msg.Write(p.Position.Z);
            }
        }

        public override void Read(NetIncomingMessage msg)
        {
            int count = msg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var p = new Player();
                p.ID = msg.ReadInt32();
                p.Name = msg.ReadString();
                var hex = msg.ReadString();
                p.Color = ColorHelper.HexToVector(hex);
                float x = msg.ReadFloat();
                float y = msg.ReadFloat();
                float z = msg.ReadFloat();
                p.Position = new Vector3(x, y,z);
                p.DisplayedPosition = p.Position;
                Players[p.ID] = p;
            }
        }
    }
}
