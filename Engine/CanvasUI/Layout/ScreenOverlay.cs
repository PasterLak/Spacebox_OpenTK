namespace Engine.CanvasUI;

public abstract class ScreenOverlay : AreaControl
{
    protected override Rect2 ComputeContentRect(Rect2 rect) => rect;
}
