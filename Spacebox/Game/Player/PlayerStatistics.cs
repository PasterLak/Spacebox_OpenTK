using Engine;
using System.Text;
using System.Text.Json.Serialization;

namespace Spacebox.Game.Player
{
    public class PlayerStatistics
    {
        public const int MaxTrackedKeys = 64;
        public const string OtherKey = "Other";
        public const string UnknownDeathCause = "Unknown";

        public int TotalPlayTimeMinutes { get; set; } = 0;
        public double PlayTimeSeconds { get; set; } = 0;
        public int SessionsPlayed { get; set; } = 0;
        public DateTime FirstPlayedUtc { get; set; }
        public DateTime LastPlayedUtc { get; set; } = DateTime.UtcNow;

        public int BlocksDestroyed { get; set; } = 0;
        public int BlocksPlaced { get; set; } = 0;
        public int ItemsCrafted { get; set; } = 0;
        public int ItemsProcessed { get; set; } = 0;
        public int ItemsPickedUp { get; set; } = 0;
        public int ItemsConsumed { get; set; } = 0;

        public int ShotsFired { get; set; } = 0;
        public int ShotsHit { get; set; } = 0;
        public int ProjectilesRicocheted { get; set; } = 0;
        public int ExplosionsCaused { get; set; } = 0;
        public long BlockDamageDealt { get; set; } = 0;
        public double EntityDamageDealt { get; set; } = 0;

        public int DeathsTotal { get; set; } = 0;
        public int DamageTaken { get; set; } = 0;
        public int HealthHealed { get; set; } = 0;

        public double DistanceTraveled { get; set; } = 0;
        public double DistanceInShips { get; set; } = 0;
        public float MaxSpeedReached { get; set; } = 0;
        public int TeleportsUsed { get; set; } = 0;

        public HashSet<long> VisitedAsteroids { get; set; } = new HashSet<long>();
        public HashSet<string> VisitedSectors { get; set; } = new HashSet<string>();
        public int FlashlightToggles { get; set; } = 0;

        public int AsteroidsDiscovered
        {
            get => VisitedAsteroids?.Count ?? 0;
            set { }
        }

        public int SectorsExplored
        {
            get => VisitedSectors?.Count ?? 0;
            set { }
        }

        public Dictionary<string, int> ItemsUsed { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DeathCauses { get; set; } = new Dictionary<string, int>();

        [JsonIgnore]
        public int SessionMinutes => (int)(_sessionSeconds / 60);

        private double _sessionSeconds;

        public void Normalize()
        {
            VisitedAsteroids ??= new HashSet<long>();
            VisitedSectors ??= new HashSet<string>();
            ItemsUsed ??= new Dictionary<string, int>();
            DeathCauses ??= new Dictionary<string, int>();

            if (!double.IsFinite(PlayTimeSeconds) || PlayTimeSeconds < 0) PlayTimeSeconds = 0;
            if (!double.IsFinite(DistanceTraveled) || DistanceTraveled < 0) DistanceTraveled = 0;
            if (!double.IsFinite(DistanceInShips) || DistanceInShips < 0) DistanceInShips = 0;
            if (!double.IsFinite(EntityDamageDealt) || EntityDamageDealt < 0) EntityDamageDealt = 0;
            if (!float.IsFinite(MaxSpeedReached) || MaxSpeedReached < 0) MaxSpeedReached = 0;
            if (PlayTimeSeconds <= 0 && TotalPlayTimeMinutes > 0) PlayTimeSeconds = TotalPlayTimeMinutes * 60.0;
        }

        public void StartSession() => StartSession(DateTime.UtcNow);

        public void StartSession(DateTime nowUtc)
        {
            Normalize();
            if (FirstPlayedUtc == default) FirstPlayedUtc = nowUtc;
            LastPlayedUtc = nowUtc;
            SessionsPlayed++;
            _sessionSeconds = 0;
        }

        public void AddPlayTime(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds <= 0) return;
            PlayTimeSeconds += seconds;
            _sessionSeconds += seconds;
            TotalPlayTimeMinutes = (int)(PlayTimeSeconds / 60);
        }

        public void PrepareForSave() => PrepareForSave(DateTime.UtcNow);

        public void PrepareForSave(DateTime nowUtc)
        {
            Normalize();
            TotalPlayTimeMinutes = (int)(PlayTimeSeconds / 60);
            LastPlayedUtc = nowUtc;
        }

        public void AddDistance(double meters, bool inShip)
        {
            if (!double.IsFinite(meters) || meters <= 0) return;
            DistanceTraveled += meters;
            if (inShip) DistanceInShips += meters;
        }

        public void RecordSpeed(float speed)
        {
            if (float.IsFinite(speed) && speed > MaxSpeedReached) MaxSpeedReached = speed;
        }

        public bool RecordAsteroid(long entityId)
        {
            VisitedAsteroids ??= new HashSet<long>();
            return VisitedAsteroids.Add(entityId);
        }

        public bool RecordSector(string sectorKey)
        {
            if (string.IsNullOrEmpty(sectorKey)) return false;
            VisitedSectors ??= new HashSet<string>();
            return VisitedSectors.Add(sectorKey);
        }

        public void RecordDeath(string cause)
        {
            DeathsTotal++;
            DeathCauses ??= new Dictionary<string, int>();
            Increment(DeathCauses, string.IsNullOrWhiteSpace(cause) ? UnknownDeathCause : cause.Trim());
        }

        public void RecordItemUse(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            ItemsUsed ??= new Dictionary<string, int>();
            Increment(ItemsUsed, itemId);
        }

        public void AddProcessedItems(int count)
        {
            if (count > 0) ItemsProcessed += count;
        }

        public static int CountMovedFrom(Item itemBefore, int countBefore, Item itemAfter, int countAfter)
        {
            if (itemBefore == null || countBefore <= 0) return 0;
            int left = itemAfter == itemBefore ? countAfter : 0;
            return Math.Max(0, countBefore - left);
        }

        public void RecordDamageTaken(int damage)
        {
            if (damage > 0) DamageTaken += damage;
        }

        public void RecordEntityDamage(double damage)
        {
            if (double.IsFinite(damage) && damage > 0) EntityDamageDealt += damage;
        }

        public float GetAccuracy()
        {
            return ShotsFired > 0 ? MathF.Min(100f, (float)ShotsHit / ShotsFired * 100f) : 0f;
        }

        public int GetAverageSessionTimeMinutes()
        {
            return SessionsPlayed > 0 ? (int)(PlayTimeSeconds / 60 / SessionsPlayed) : 0;
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            if (!counts.ContainsKey(key) && counts.Count >= MaxTrackedKeys) key = OtherKey;
            counts.TryGetValue(key, out int value);
            counts[key] = value + 1;
        }

        public void Print()
        {
            var sb = new StringBuilder();

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
              .Append(ItemsCrafted).Append(" crafted, ").Append(ItemsConsumed)
              .AppendLine(" consumed");

            sb.Append("Health: ").Append(DamageTaken).Append(" damage taken, ")
              .Append(EntityDamageDealt.ToString("F0")).Append(" dealt, ").Append(HealthHealed)
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
