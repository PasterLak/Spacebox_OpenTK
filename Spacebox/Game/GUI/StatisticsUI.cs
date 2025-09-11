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
        DrawStatRow("Play Time", $"{_statistics.TotalPlayTimeMinutes} min ({_statistics.SessionsPlayed} sessions)", labelWidth);
        DrawStatRow("Average Session", $"{_statistics.GetAverageSessionTimeMinutes()} minutes", labelWidth);
        DrawStatRow("Game time", $"{GameTime.ToString()}", labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Building", listSize.X, categoryColor);
        DrawStatRow("Blocks Placed", ValueToString(_statistics.BlocksPlaced), labelWidth);
        DrawStatRow("Blocks Destroyed", ValueToString(_statistics.BlocksDestroyed), labelWidth);
        DrawStatRow("Damage to blocks", ValueToString(_statistics.BlockDamageDealt), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Items", listSize.X, categoryColor);
        DrawStatRow("Items Picked Up", ValueToString(_statistics.ItemsPickedUp), labelWidth);
        DrawStatRow("Items Crafted", ValueToString(_statistics.ItemsCrafted), labelWidth);
        DrawStatRow("Items Processed", ValueToString(_statistics.ItemsProcessed), labelWidth);
        DrawStatRow("Items Consumed", ValueToString(_statistics.ItemsСonsumed), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Health", listSize.X, categoryColor);
        DrawStatRow("Health Healed", ValueToString(_statistics.HealthHealed), labelWidth);
        DrawStatRow("Damage Taken", ValueToString(_statistics.DamageTaken), labelWidth);
        DrawStatRow("Deaths", _statistics.DeathsTotal.ToString(), labelWidth);

        ImGui.Spacing();
        DrawCategoryHeader("Combat", listSize.X, categoryColor);
        DrawStatRow("Shots Fired", ValueToString(_statistics.ShotsFired), labelWidth);
        DrawStatRow("Accuracy", $"{_statistics.GetAccuracy():F1}%", labelWidth);
        DrawStatRow("Ricochets", ValueToString(_statistics.ProjectilesRicocheted), labelWidth);
        DrawStatRow("Explosions", ValueToString(_statistics.ExplosionsCaused), labelWidth);
        DrawStatRow("Damage dealt", ValueToString(_statistics.EntityDamageDealt), labelWidth);

        

       

        ImGui.Spacing();
        DrawCategoryHeader("Exploration", listSize.X, categoryColor);
        DrawStatRow("Distance Traveled", ValueToString(_statistics.DistanceTraveled) + " m.", labelWidth);
        DrawStatRow("Max Speed", $"{_statistics.MaxSpeedReached}", labelWidth);
        DrawStatRow("Asteroids Found", _statistics.AsteroidsDiscovered.ToString(), labelWidth);
    }

    
    private static string ValueToString(int value)
    {
        return value.ToString("N0").Replace(",", " ");
    }
    private static string ValueToString(long value)
    {
        return value.ToString("N0").Replace(",", " ");
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