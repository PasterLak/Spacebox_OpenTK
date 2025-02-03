

namespace CommonLibrary
{
    public static class Settings
    {
        public static int Port = 14242;
        public static int MaxPlayers = 100;
        public static string Name = "My Spacebox Server";
        public static string Key = "";
        public static string Ip = " 192.168.56.1"; // "192.168.0.102"
        public static float PingInterval = 0.5f;
        public static float ConnectionTimeout = 2f;
        public static int TimeToCheckAfk = 10;
        public static int TimeCanBeAfkSec = 60 * 5;

        public static Dictionary<string, string> ServerDataToDictionary()
        {
            Dictionary<string,string> dict = new Dictionary<string,string>();

            dict.Add("port", Port.ToString());
            dict.Add("key", Key.ToString());
            dict.Add("maxPlayers", MaxPlayers.ToString());
            dict.Add("pingInterval", PingInterval.ToString());
            dict.Add("connectionTimeout", ConnectionTimeout.ToString());
           // dict.Add("timeToCheckIfPlayerAfkSec", TimeToCheckAfk.ToString());
          //  dict.Add("connectionTimeout", TimeCanBeAfkSec.ToString());

            return dict;
        }

        public static Dictionary<string, string> ClientDataToDictionary()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("port", Port.ToString());
            dict.Add("key", Key.ToString());
            dict.Add("ip", Ip.ToString());
         

            return dict;
        }

    }
}
