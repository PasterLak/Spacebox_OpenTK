using Engine;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using System.Numerics;

namespace Spacebox.Game.GUI;

public static class StatisticsUI
{
    private static bool _isVisible = false;
    private static PlayerStatistics _statistics;

    public static bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    public static void Show(PlayerStatistics statistics)
    {
        _statistics = statistics;
        _isVisible = true;
    }

    public static void Hide()
    {
        _isVisible = false;
        _statistics = null;
    }

    public static void OnGUI()
    {
        if (!_isVisible || _statistics == null) return;

        SettingsUI.Render("StatisticsWindow", "STATISTICS", 0,
            (listSize, rowHeight) => DrawStatistics(listSize, rowHeight),
            null,
            () => Hide());
    }
    static Vector4 categoryColor = Color3Byte.Yellow.ToVector4().ToSystemVector4();
    private static void DrawStatistics(Vector2 listSize, float rowHeight)
    {
        float labelWidth = listSize.X * 0.6f;
       

        DrawCategoryHeader("General", listSize.X, categoryColor);
        DrawStatRow("Play Time", $"{FormatDuration(_statistics.PlayTimeSeconds)} ({_statistics.SessionsPlayed} sessions)", labelWidth);
        DrawStatRow("This Session", $"{_statistics.SessionMinutes} minutes", labelWidth);
        DrawStatRow("Average Session", $"{_statistics.GetAverageSessionTimeMinutes()} minutes", labelWidth);
        DrawStatRow("Game time", $"{GameTime.ToString()}", labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Building", listSize.X, categoryColor);
        DrawStatRow("Blocks Placed", ValueToString(_statistics.BlocksPlaced), labelWidth);
        DrawStatRow("Blocks Destroyed", ValueToString(_statistics.BlocksDestroyed), labelWidth);
        DrawStatRow("Damage To Blocks", ValueToString(_statistics.BlockDamageDealt), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Items", listSize.X, categoryColor);
        DrawStatRow("Picked Up", ValueToString(_statistics.ItemsPickedUp), labelWidth);
        DrawStatRow("Crafted", ValueToString(_statistics.ItemsCrafted), labelWidth);
        DrawStatRow("Processed", ValueToString(_statistics.ItemsProcessed), labelWidth);
        DrawStatRow("Consumed", ValueToString(_statistics.ItemsConsumed), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Health", listSize.X, categoryColor);
        DrawStatRow("Health Healed", ValueToString(_statistics.HealthHealed), labelWidth);
        DrawStatRow("Damage Taken", ValueToString(_statistics.DamageTaken), labelWidth);
        DrawStatRow("Deaths", _statistics.DeathsTotal.ToString(), labelWidth);
        foreach (var cause in _statistics.DeathCauses)
            DrawStatRow("   " + cause.Key, ValueToString(cause.Value), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Combat", listSize.X, categoryColor);
        DrawStatRow("Shots", ValueToString(_statistics.ShotsFired), labelWidth);
        DrawStatRow("Hits", ValueToString(_statistics.ShotsHit), labelWidth);
        DrawStatRow("Accuracy", $"{_statistics.GetAccuracy():F1}%", labelWidth);
        DrawStatRow("Ricochets", ValueToString(_statistics.ProjectilesRicocheted), labelWidth);
        DrawStatRow("Explosions", ValueToString(_statistics.ExplosionsCaused), labelWidth);
        DrawStatRow("Damage dealt", ValueToString(_statistics.EntityDamageDealt), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Exploration", listSize.X, categoryColor);
        DrawStatRow("Distance Traveled", FormatDistance(_statistics.DistanceTraveled), labelWidth);
        DrawStatRow("Distance In Ships", FormatDistance(_statistics.DistanceInShips), labelWidth);
        DrawStatRow("Max Speed", $"{_statistics.MaxSpeedReached:F1} m/s", labelWidth);
        DrawStatRow("Asteroids Visited", ValueToString(_statistics.AsteroidsDiscovered), labelWidth);
        DrawStatRow("Sectors Visited", ValueToString(_statistics.SectorsExplored), labelWidth);
        DrawStatRow("Teleports", ValueToString(_statistics.TeleportsUsed), labelWidth);
        //DrawStatRow("Max Speed", $"{_statistics.MaxSpeedReached}", labelWidth);
        //DrawStatRow("Asteroids Found", _statistics.AsteroidsDiscovered.ToString(), labelWidth);
    }

    
    private static string ValueToString(int value)
    {
        return value.ToString("N0").Replace(",", " ");
    }
    private static string ValueToString(long value)
    {
        return value.ToString("N0").Replace(",", " ");
    }

    private static string ValueToString(double value)
    {
        return ValueToString((long)Math.Round(value));
    }

    private static string FormatDistance(double meters)
    {
        return meters >= 10000 ? $"{meters / 1000:F1} km" : ValueToString(meters) + " m";
    }

    private static string FormatDuration(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return time.TotalHours >= 1 ? $"{(int)time.TotalHours} h {time.Minutes} min" : $"{time.Minutes} min";
    }

    private static void DrawCategoryHeader(string category, float width, Vector4 color)
    {
        Vector2 textSize = ImGui.CalcTextSize(category);
        //float centerX = (width - textSize.X) * 0.5f;
        float centerX = 0f;
        ImGui.SetCursorPosX(centerX);
        ImGui.TextColored(color, category);
        ImGui.Separator();
    }

    private static void DrawStatRow(string label, string value, float labelWidth)
    {
        ImGui.Text(label);
        ImGui.SameLine(labelWidth);
        ImGui.Text(value);
    }
}