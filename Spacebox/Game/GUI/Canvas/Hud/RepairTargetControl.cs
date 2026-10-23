using Engine.CanvasUI;
using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class RepairTargetControl : HudOverlayControl
{
    private static readonly Color4 FullColor = new Color4(0.43f, 0.86f, 0.43f, 1f);
    private static readonly Color4 HalfColor = new Color4(1f, 0.77f, 0.16f, 1f);
    private static readonly Color4 LowColor = new Color4(1f, 0.42f, 0.35f, 1f);
    private static readonly Color4 BarBackground = new Color4(0.18f, 0.2f, 0.24f, 0.78f);

    private readonly RepairTargetUI _repairTarget;

    public RepairTargetControl(RepairTargetUI repairTarget)
    {
        _repairTarget = repairTarget;
    }

    protected override bool IsShown() => _repairTarget.IsVisible && !UIManager.IsUIMode;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        float unit = Unit;
        var center = Center + new Vector2(0f, unit * 5f);
        float lineHeight = LineHeight;
        DrawCenteredText(renderer, center.X, center.Y, _repairTarget.Name, TextColor);

        float width = MathF.Round(unit * 18f);
        float height = MathF.Max(4f, MathF.Round(unit * 1.2f));
        var bar = new Rect2(MathF.Round(center.X - width * 0.5f), MathF.Round(center.Y + lineHeight + unit * 0.6f), width, height);
        float fill = _repairTarget.Fill;
        var color = fill >= 1f ? FullColor : fill > 0.5f ? HalfColor : LowColor;
        renderer.DrawRect(bar, BarBackground);
        renderer.DrawRect(new Rect2(bar.X, bar.Y, MathF.Round(width * fill), height), color);
        CanvasDraw.Outline(renderer, bar, Color4.Black, 1f, 1f, 1f, 1f);

        DrawCenteredText(renderer, center.X, bar.Y + height + unit * 0.5f, _repairTarget.Status, _repairTarget.IsRepairing ? color : TextColor);
    }
}
