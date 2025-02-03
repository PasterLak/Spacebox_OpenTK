using SpaceNetwork;


namespace CommonLibrary
{
    public static class ConfigManager
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Settings.Key = "defaultKey";
                SaveConfig();
                return;
            }
            try
            {
                var config = KeyValueFileReader.ReadFile(ConfigFilePath);
                if (config.ContainsKey("port") && int.TryParse(config["port"], out int port))
                    Settings.Port = port;
                if (config.ContainsKey("key") && !string.IsNullOrWhiteSpace(config["key"]))
                    Settings.Key = config["key"];
                else
                {
                    Settings.Key = "defaultKey";
                    SaveConfig();
                }
                if (config.ContainsKey("name") && !string.IsNullOrWhiteSpace(config["name"]))
                    Settings.Name = config["name"];
                else
                {
                    Settings.Name = "MyServer";
                    SaveConfig();
                }
                if (config.ContainsKey("maxPlayers") && int.TryParse(config["maxPlayers"], out int maxPlayers))
                    Settings.MaxPlayers = maxPlayers;
                if (config.ContainsKey("pingInterval") && float.TryParse(config["pingInterval"], out float pingInterval))
                    Settings.PingInterval = pingInterval;
                if (config.ContainsKey("connectionTimeout") && float.TryParse(config["connectionTimeout"], out float connectionTimeout))
                    Settings.ConnectionTimeout = connectionTimeout;
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading configuration", ex);
            }
        }

        public static void SaveConfig()
        {
            var dict = Settings.ServerDataToDictionary();
            KeyValueFileWriter.WriteFile(ConfigFilePath, dict);
        }
    }
}
