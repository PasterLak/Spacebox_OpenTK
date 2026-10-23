using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class SelectedItemNameControl : HudOverlayControl
{
    public Control Above { get; set; }

    protected override bool IsShown() => base.IsShown() && Above != null && Above.Visible && !string.IsNullOrEmpty(PanelUI.ShownItemName);

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        float top = Above.GlobalPosition.Y - GlobalPosition.Y;
        DrawCenteredText(renderer, Center.X, top - LineHeight - 6f, PanelUI.ShownItemName, TextColor);
    }
}
