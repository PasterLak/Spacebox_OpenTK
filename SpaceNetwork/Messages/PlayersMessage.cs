
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
                msg.WriteObject(p.Position);
                msg.WriteObject(p.Rotation);
                msg.Write(p.Name);
                msg.Write(ColorHelper.VectorToHex(p.Color));
                msg.Write(p.SkinColor);
                
               
            }
        }

        public override void Read(NetIncomingMessage msg)
        {
            int count = msg.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var p = new Player();

                p.ID = msg.ReadInt32();
                p.Position = msg.ReadObject<Vector3>();
                p.Rotation = msg.ReadObject<Quaternion>();
                p.Name = msg.ReadString();
                p.Color = ColorHelper.HexToVector(msg.ReadString());
                p.SkinColor = msg.ReadString();
              
               
                p.DisplayedPosition = p.Position;
                Players[p.ID] = p;
            }
        }
    }
}
