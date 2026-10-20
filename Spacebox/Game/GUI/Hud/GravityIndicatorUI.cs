using System.Numerics;
using ImGuiNET;

namespace Spacebox.Game.GUI;

public static class GravityIndicatorUI
{
    private const string IconName = "gravity";
    private const float SizeRatio = 0.05f;
    private const float MarginRatio = 0.02f;

    public static void Draw(bool isInGravity)
    {
        if (!isInGravity || !Settings.ShowInterface) return;

        var icon = UIAtlas.Get(IconName);
        if (!icon.IsValid) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        float size = displaySize.Y * SizeRatio;
        float margin = displaySize.Y * MarginRatio;
        var min = new Vector2(displaySize.X - size - margin, displaySize.Y - size - margin);

        icon.Draw(ImGui.GetForegroundDrawList(), min, min + new Vector2(size));
    }
}
