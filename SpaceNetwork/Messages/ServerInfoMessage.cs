using Lidgren.Network;

namespace SpaceNetwork.Messages
{
    public class ServerInfoMessage : BaseMessage
    {
        public ServerInfo Info { get; set; }
        protected override void WriteData(NetOutgoingMessage msg)
        {
            msg.Write(Info.Name ?? "");
            msg.Write(Info.Description ?? "");
            msg.Write(Info.MaxPlayers);
        }
        public override void Read(NetIncomingMessage msg)
        {
            Info = new ServerInfo
            {
                Name = msg.ReadString(),
                Description = msg.ReadString(),
                MaxPlayers = msg.ReadInt32()
            };
        }
    }
}
