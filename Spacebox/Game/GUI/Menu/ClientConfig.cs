
using Spacebox.Client;
using SpaceNetwork;

namespace Spacebox.Game.GUI.Menu
{
    public class ClientConfig
    {
        public string PlayerNickname { get; set; } = "";
        public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();



        public bool GenerateNameIfEmpty()
        {
            if(PlayerNickname == "")
            {
                Random r = new Random();
                PlayerNickname = "Player" + r.Next(0, 1000);
                
                return true;
            }


            return false;
        }
    }
}
