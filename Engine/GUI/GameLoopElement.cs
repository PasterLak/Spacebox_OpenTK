using Engine;
using Engine.GUI;
using ImGuiNET;
using System.Numerics;

internal class GameLoopElement : OverlayElement
{
    private float timer;
    private float cachedRenderTime;
    private float cachedAvgRenderTime;
    private float cachedUpdateTime;
    private float cachedAvgUpdateTime;
    private float cachedOnGUITime;
    private float cachedAvgOnGUITime;
    private float cachedRenderPercent;
    private float cachedUpdatePercent;
    private float cachedOnGUIpercent;


public override void OnGUIText()
    {
        timer += Time.Delta;
        if (timer >= 1f)
        {
            cachedAvgRenderTime = (float)Time.AverageRenderTime;
            cachedAvgUpdateTime = (float)Time.AverageUpdateTime;
            cachedAvgOnGUITime = (float)Time.AverageOnGUITime;
            cachedRenderPercent = Time.RenderTimePercent;
            cachedUpdatePercent = Time.UpdateTimePercent;
            cachedOnGUIpercent = Time.OnGUITimePercent;
            timer = 0f;
        }

        ImGui.Text(" ");
        ImGui.Text("[GAME LOOP DEBUG]");
        ImGui.Text(" ");
        ImGui.Text($"Avg Render Time: {cachedAvgRenderTime:F2} ms");
        ImGui.Text($"Avg Update Time: {cachedAvgUpdateTime:F2} ms");
        ImGui.Text($"Avg OnGUI Time: {cachedAvgOnGUITime:F2} ms");
        ImGui.Text(" ");
        ImGui.Text($"FrameLimiter (F7): {(FrameLimiter.TargetFPS == 120)}");
        ImGui.Text(" ");

        DrawPercentBar("Render", cachedRenderPercent); 
        DrawPercentBar("Update", cachedUpdatePercent); 
        DrawPercentBar("OnGUI", cachedOnGUIpercent);
        ImGui.Text(" ");
    }

    private void DrawPercentBar(string label, float percent)
    {
        ImGui.Text($"{label}: {percent:F0}");
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        const float width = 200f;
        const float height = 8f;
        float segmentWidth = width / 100f;
        int filled = (int)percent;

        for (int i = 0; i < 100; i++)
        {
            var x0 = pos.X + i * segmentWidth;
            var y0 = pos.Y;
            var x1 = x0 + segmentWidth - 1;
            var y1 = y0 + height;
            uint col = (uint)ImGui.GetColorU32(i < filled
                ? ImGuiCol.PlotHistogram  // or Green
                : ImGuiCol.FrameBg        // or Grey
            );
            drawList.AddRectFilled(new Vector2(x0, y0), new Vector2(x1, y1), col);
        }
        
        ImGui.Dummy(new Vector2(width, height));
        ImGui.Text(" ");
    }


}
