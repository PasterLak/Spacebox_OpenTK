using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ServerCommon
{
    public static class BanManager
    {
        private static readonly string BanFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "banned.json");
        private static Dictionary<int, PlayerBanned> bannedPlayers = new Dictionary<int, PlayerBanned>();

        public static void LoadBannedPlayers()
        {
            if (File.Exists(BanFilePath))
            {
                string json = File.ReadAllText(BanFilePath);
                bannedPlayers = JsonSerializer.Deserialize<Dictionary<int, PlayerBanned>>(json);
                if (bannedPlayers == null) bannedPlayers = new Dictionary<int, PlayerBanned>();
            }
        }

        public static void SaveBannedPlayers()
        {
            string json = JsonSerializer.Serialize(bannedPlayers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BanFilePath, json);
        }

        public static void AddBannedPlayer(PlayerBanned banned)
        {
            bannedPlayers[banned.IDWhenWasBanned] = banned;
            SaveBannedPlayers();
        }

        public static bool IsBanned(int playerId)
        {
            return bannedPlayers.ContainsKey(playerId);
        }

        public static IEnumerable<PlayerBanned> GetAllBannedPlayers()
        {
            return bannedPlayers.Values;
        }
    }
}
