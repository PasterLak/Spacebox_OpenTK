using Engine.CanvasUI;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class SelectedItemNameControl : HudOverlayControl
{
    private readonly PanelController _panel;

    public SelectedItemNameControl(PanelController panel)
    {
        _panel = panel;
    }

    public Control Above { get; set; }

    protected override bool IsShown() => base.IsShown() && Above != null && Above.Visible && !string.IsNullOrEmpty(_panel.ShownItemName);

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        float top = Above.GlobalPosition.Y - GlobalPosition.Y;
        DrawCenteredText(renderer, Center.X, top - LineHeight - 6f, _panel.ShownItemName, TextColor);
    }
}
