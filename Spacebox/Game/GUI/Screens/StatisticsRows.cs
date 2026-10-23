using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public readonly record struct StatisticsRow(string Label, string Value, bool IsHeader);

public static class StatisticsRows
{
    public static List<StatisticsRow> Build(PlayerStatistics statistics, string gameTime)
    {
        var rows = new List<StatisticsRow>();
        if (statistics == null) return rows;

        Header(rows, "General");
        Row(rows, "Play Time", $"{FormatDuration(statistics.PlayTimeSeconds)} ({statistics.SessionsPlayed} sessions)");
        Row(rows, "This Session", $"{statistics.SessionMinutes} minutes");
        Row(rows, "Average Session", $"{statistics.GetAverageSessionTimeMinutes()} minutes");
        Row(rows, "Game time", gameTime ?? "");

        Header(rows, "Building");
        Row(rows, "Blocks Placed", Number(statistics.BlocksPlaced));
        Row(rows, "Blocks Destroyed", Number(statistics.BlocksDestroyed));
        Row(rows, "Damage To Blocks", Number(statistics.BlockDamageDealt));

        Header(rows, "Items");
        Row(rows, "Picked Up", Number(statistics.ItemsPickedUp));
        Row(rows, "Crafted", Number(statistics.ItemsCrafted));
        Row(rows, "Processed", Number(statistics.ItemsProcessed));
        Row(rows, "Consumed", Number(statistics.ItemsConsumed));

        Header(rows, "Health");
        Row(rows, "Health Healed", Number(statistics.HealthHealed));
        Row(rows, "Damage Taken", Number(statistics.DamageTaken));
        Row(rows, "Deaths", statistics.DeathsTotal.ToString());
        foreach (var cause in statistics.DeathCauses)
            Row(rows, "   " + cause.Key, Number(cause.Value));

        Header(rows, "Combat");
        Row(rows, "Shots", Number(statistics.ShotsFired));
        Row(rows, "Hits", Number(statistics.ShotsHit));
        Row(rows, "Accuracy", $"{statistics.GetAccuracy():F1}%");
        Row(rows, "Ricochets", Number(statistics.ProjectilesRicocheted));
        Row(rows, "Explosions", Number(statistics.ExplosionsCaused));
        Row(rows, "Damage dealt", Number(statistics.EntityDamageDealt));

        Header(rows, "Exploration");
        Row(rows, "Distance Traveled", FormatDistance(statistics.DistanceTraveled));
        Row(rows, "Distance In Ships", FormatDistance(statistics.DistanceInShips));
        Row(rows, "Max Speed", $"{statistics.MaxSpeedReached:F1} m/s");
        Row(rows, "Asteroids Visited", Number(statistics.AsteroidsDiscovered));
        Row(rows, "Sectors Visited", Number(statistics.SectorsExplored));
        Row(rows, "Teleports", Number(statistics.TeleportsUsed));
        //DrawStatRow("Max Speed", $"{_statistics.MaxSpeedReached}", labelWidth);
        //DrawStatRow("Asteroids Found", _statistics.AsteroidsDiscovered.ToString(), labelWidth);
        return rows;
    }

    public static string Number(long value)
    {
        return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
    }

    public static string Number(double value)
    {
        return Number((long)Math.Round(value));
    }

    public static string FormatDistance(double meters)
    {
        return meters >= 10000 ? $"{meters / 1000:F1} km" : Number(meters) + " m";
    }

    public static string FormatDuration(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min" : $"{time.Minutes} min";
    }

    private static void Header(List<StatisticsRow> rows, string text) => rows.Add(new StatisticsRow(text, "", true));

    private static void Row(List<StatisticsRow> rows, string label, string value) => rows.Add(new StatisticsRow(label, value, false));
}
