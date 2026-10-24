using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Ships;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class ShipHudControl : HudOverlayControl
{
    private const float PanelPadding = 6f;
    private const float PanelTop = 0.78f;
    private static readonly Color4 PanelColor = new Color4(0f, 0f, 0f, 0.35f);
    private static readonly Color4 LabelBackground = new Color4(0f, 0f, 0f, 0.6f);

    private readonly List<ShipBindingTool.Label> _labels = new List<ShipBindingTool.Label>();

    private readonly ShipConsoleUI _shipConsole;

    public ShipHudControl(ShipConsoleUI shipConsole)
    {
        _shipConsole = shipConsole;
    }

    protected override bool IsShown() => ShipPilot.Console?.Entity != null && !Engine.UIManager.IsUIMode;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        DrawLabels(renderer);

        string text = _shipConsole.HudText;
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split('\n');
        float lineHeight = LineHeight;
        float width = 0f;
        foreach (var line in lines) width = MathF.Max(width, Measure(line).X);
        var panel = new Rect2(MathF.Round((LocalRect.Width - width) * 0.5f - PanelPadding), MathF.Round(LocalRect.Height * PanelTop), width + PanelPadding * 2f, lines.Length * lineHeight + PanelPadding * 2f);
        StyleBoxFlat.FillRounded(renderer, panel, 4f, PanelColor);
        for (int i = 0; i < lines.Length; i++) DrawText(renderer, new Vector2(panel.X + PanelPadding, panel.Y + PanelPadding + i * lineHeight), lines[i], TextColor);
    }

    private void DrawLabels(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        if (Root == null || font == null) return;
        var binding = GameScene.Current?.Binding;
        if (binding != null) binding.CollectLabels(_labels, ShipPilot.Console);
        else _labels.Clear();
        foreach (var label in _labels)
        {
            var point = Root.ScreenToCanvas(label.Screen) - GlobalPosition;
            var size = Measure(label.Text);
            var box = new Rect2(MathF.Round(point.X - size.X * 0.5f - 4f), MathF.Round(point.Y - size.Y * 0.5f - 2f), size.X + 8f, size.Y + 4f);
            StyleBoxFlat.FillRounded(renderer, box, 3f, LabelBackground);
            CanvasDraw.Text(renderer, font, new Vector2(box.X + 4f, box.Y + 2f), label.Text, GetThemeFontSize(), label.Color);
        }
    }
}
