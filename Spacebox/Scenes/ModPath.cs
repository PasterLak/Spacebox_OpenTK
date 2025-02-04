

using Spacebox.Game;

namespace Spacebox.Scenes
{
    public class ModPath
    {

        public ModPath() { }


        public static string GetModsPath(bool localGame, string serverName)
        {
            if(localGame) {

                return Globals.GameSet.LocalFolder;
            }

            return Path.Combine(Globals.GameSet.MultiplayerFolder, serverName) ;
        }
    }
}
