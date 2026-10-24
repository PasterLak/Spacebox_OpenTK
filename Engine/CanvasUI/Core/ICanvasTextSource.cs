namespace Engine.CanvasUI;

public readonly record struct CanvasTextRun(string Text, Rect2 Rect);

public interface ICanvasTextSource
{
    void CollectText(List<CanvasTextRun> runs);
}
