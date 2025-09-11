
using Engine;
using OpenTK.Mathematics;
using System.Text;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Player
{
    public class PlayerStatistics
    {
        public int TotalPlayTimeMinutes { get; set; } = 0;
        public int SessionsPlayed { get; set; } = 0;
        public DateTime FirstPlayedUtc { get; set; }
        public DateTime LastPlayedUtc { get; set; } = DateTime.UtcNow;

        public int BlocksDestroyed { get; set; } = 0;
        public int BlocksPlaced { get; set; } = 0;
        public int ItemsCrafted { get; set; } = 0;
        public int ItemsProcessed { get; set; } = 0;
        public int ItemsPickedUp { get; set; } = 0;
        public int ItemsСonsumed { get; set; } = 0;

        public int ShotsFired { get; set; } = 0;
        public int ShotsHit { get; set; } = 0;
        public int ProjectilesRicocheted { get; set; } = 0;
        public int ExplosionsCaused { get; set; } = 0;
        public long BlockDamageDealt { get; set; } = 0;
        public long EntityDamageDealt { get; set; } = 0;

        public int DeathsTotal { get; set; } = 0;
        public int DamageTaken { get; set; } = 0;
        public int HealthHealed { get; set; } = 0;

        public long DistanceTraveled { get; set; } = 0;
        public int MaxSpeedReached { get; set; } = 0;
        public int TeleportsUsed { get; set; } = 0;

        public int AsteroidsDiscovered { get; set; } = 0;
        public int SectorsExplored { get; set; } = 0;
        public int FlashlightToggles { get; set; } = 0;

        public Dictionary<string, int> ItemsUsed { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DeathCauses { get; set; } = new Dictionary<string, int>();

        public void UpdateDistance(Vector3 lastPos, Vector3 currentPos)
        {
            var dis = (long)Vector3.Distance(currentPos, lastPos);
            DistanceTraveled += dis;
        }

        [JsonIgnore]
        private DateTime _sessionStartTime;

        public void StartSession()
        {
            _sessionStartTime = DateTime.UtcNow;
        }

        public void EndSession()
        {
            var sessionDuration = (int)(DateTime.UtcNow - _sessionStartTime).TotalMinutes;
            TotalPlayTimeMinutes += sessionDuration;
            SessionsPlayed++;
            LastPlayedUtc = DateTime.UtcNow;
        }

        public float GetAccuracy()
        {
            return ShotsFired > 0 ? (float)ShotsHit / ShotsFired * 100f : 0f;
        }

        public int GetAverageSessionTimeMinutes()
        {
            return SessionsPlayed > 0 ? TotalPlayTimeMinutes / SessionsPlayed : 0;
        }



        public void Print()
        {
            var sb = new StringBuilder();

            LastPlayedUtc = DateTime.UtcNow;

            sb.AppendLine("=== PLAYER STATISTICS ===");
            sb.Append("Play Time: ").Append(TotalPlayTimeMinutes).Append(" min (")
              .Append(SessionsPlayed).Append(" sessions, avg ")
              .Append(GetAverageSessionTimeMinutes()).AppendLine(" min)");

            sb.Append("Combat: ").Append(ShotsFired).Append(" shots (")
              .Append(GetAccuracy().ToString("F1")).Append(" accuracy), ")
              .Append(ShotsHit).Append(" hits, ").Append(ProjectilesRicocheted)
              .Append(" ricochets, ").Append(ExplosionsCaused).AppendLine(" explosions");

            sb.Append("Building: ").Append(BlocksPlaced).Append(" placed, ")
              .Append(BlocksDestroyed).AppendLine(" destroyed");

            sb.Append("Items: ").Append(ItemsPickedUp).Append(" picked up, ")
              .Append(ItemsCrafted).Append(" crafted, ").Append(ItemsСonsumed)
              .AppendLine(" consumed");

            sb.Append("Health: ").Append(DamageTaken).Append(" damage taken, ")
             .Append(" dealt, ").Append(HealthHealed)
              .Append(" healed, ").Append(DeathsTotal).AppendLine(" deaths");

            sb.Append("Movement: ").Append(DistanceTraveled.ToString("F1")).Append(" units, max speed ")
              .Append(MaxSpeedReached.ToString("F1")).Append(", ").Append(TeleportsUsed)
              .AppendLine(" teleports");

            sb.Append("Exploration: ").Append(AsteroidsDiscovered).Append(" asteroids, ")
              .Append(SectorsExplored).Append(" sectors, ").Append(FlashlightToggles)
              .AppendLine(" flashlight toggles");

            Debug.Log(sb.ToString());
        }
    }
}