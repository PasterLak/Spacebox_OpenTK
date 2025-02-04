
using System.Text.Json;

namespace ServerCommon
{
    public static class BanManager
    {
        private static readonly string BanFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "banned.json");
        private static List<PlayerBanned> bannedPlayers = new List<PlayerBanned>();

        public static void LoadBannedPlayers()
        {
            if (File.Exists(BanFilePath))
            {
                if(bannedPlayers != null) bannedPlayers.Clear();
                string json = File.ReadAllText(BanFilePath);
                bannedPlayers = JsonSerializer.Deserialize<List<PlayerBanned>>(json) ?? new List<PlayerBanned>();
            }
        }

        public static void SaveBannedPlayers()
        {
            string json = JsonSerializer.Serialize(bannedPlayers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BanFilePath, json);
        }

        public static void AddBannedPlayer(PlayerBanned banned)
        {
            var existing = bannedPlayers.FirstOrDefault(b => b.Name.Equals(banned.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.IDWhenWasBanned = banned.IDWhenWasBanned;
                existing.Reason = banned.Reason;
                existing.IPAddress = banned.IPAddress;
                existing.DeviceId = banned.DeviceId;
                existing.BannedAt = banned.BannedAt;
            }
            else
            {
                bannedPlayers.Add(banned);
            }
            SaveBannedPlayers();
        }

        public static bool IsBannedByName(string name)
        {
            return bannedPlayers.Any(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsBannedByIp(string ip)
        {
            return bannedPlayers.Any(b => b.IPAddress.Equals(ip, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<PlayerBanned> GetAllBannedPlayers()
        {
            return bannedPlayers;
        }
    }
}
