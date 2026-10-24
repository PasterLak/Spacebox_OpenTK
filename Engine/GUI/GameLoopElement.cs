using Engine;
using Engine.GUI;

internal class GameLoopElement : OverlayElement
{
    private float timer = 1f;
    private float cachedRenderTime;
    private float cachedAvgRenderTime;
    private float cachedUpdateTime;
    private float cachedAvgUpdateTime;
    private float cachedOnGUITime;
    private float cachedAvgOnGUITime;
    private float cachedRenderPercent;
    private float cachedUpdatePercent;
    private float cachedOnGUIpercent;


public override void Write(OverlayText text)
    {
        timer += text.Elapsed;
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
        text.Section("[GAME LOOP]");
        //ImGui.Separator();
        DrawPercentBar(text, "Render", $"Avg: {cachedAvgRenderTime:F2} ms", cachedRenderPercent);
        DrawPercentBar(text, "Update", $"Avg: {cachedAvgUpdateTime:F2} ms",cachedUpdatePercent);
        DrawPercentBar(text, "GUI   ", $"Avg: {cachedAvgOnGUITime:F2} ms", cachedOnGUIpercent);


    }

    private void DrawPercentBar(OverlayText text, string label, string avarage, float percent)
    {
        text.Line(label);
        text.Bar(percent / 100f, $"{percent:F0}%");
        text.Append(avarage);
    }


}
