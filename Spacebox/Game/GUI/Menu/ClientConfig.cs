using System.Collections.Generic;
using Spacebox.Client;

namespace Spacebox.Game.GUI.Menu
{
    public class ClientConfig
    {
        public string PlayerNickname { get; set; } = "Player";
        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
    }
}
