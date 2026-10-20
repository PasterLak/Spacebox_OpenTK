
using Spacebox.Network;

namespace Spacebox.Game.GUI.Menu
{
    public class ClientConfig
    {
        public string PlayerNickname { get; set; } = "";
        public string SkinColor { get; set; } = "Yellow";
        public List<ServerEntry> Servers { get; set; } = new List<ServerEntry>();
        public List<string> LocalServers { get; set; } = new List<string>();



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
