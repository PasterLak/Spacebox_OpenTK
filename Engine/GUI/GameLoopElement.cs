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
      
        //ImGui.Text("[GAME LOOP]");
        ImGui.SeparatorText("[GAME LOOP]");
        //ImGui.Separator();
        DrawPercentBar("Render", $"Avg: {cachedAvgRenderTime:F2} ms", cachedRenderPercent); 
        DrawPercentBar("Update", $"Avg: {cachedAvgUpdateTime:F2} ms",cachedUpdatePercent); 
        DrawPercentBar("GUI   ", $"Avg: {cachedAvgOnGUITime:F2} ms", cachedOnGUIpercent);
       
  
    }

    private void DrawPercentBar(string label, string avarage, float percent)
    {
        ImGui.Text($"{label}"); ImGui.SameLine();
        var size = ImGui.CalcTextSize(label);
        ImGui.ProgressBar( percent/100f, new Vector2(size.Y * 5, size.Y));
        ImGui.SameLine();
        ImGui.Text(avarage);
    }


}
