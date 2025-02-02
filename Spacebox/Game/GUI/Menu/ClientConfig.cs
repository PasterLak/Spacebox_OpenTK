using System.Collections.Generic;

namespace Spacebox.Game.GUI.Menu
{
    public class ClientConfig
    {
        public string PlayerNickname { get; set; } = "Player";
        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
    }
}
