using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class TagsOverlayControl : HudOverlayControl
{
    private readonly List<TagManager.ScreenTag> _tags = new List<TagManager.ScreenTag>(256);
    private float _viewportHeight = -1f;

    protected override bool IsShown() => Settings.ShowInterface && TagManager.Instance != null;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        if (Root == null || font == null) return;
        if (_viewportHeight != Root.ViewportSize.Y)
        {
            _viewportHeight = Root.ViewportSize.Y;
            Tag.SetFontSizes(Root.ViewportSize);
        }

        TagManager.Instance.CollectScreenTags(_tags);
        for (int i = 0; i < _tags.Count; i++)
        {
            var entry = _tags[i];
            float size = MathF.Round(Tag.CalculateFontSize(entry.DistanceSquared));
            var textSize = font.Measure(entry.Tag.Text, size);
            var point = Root.ScreenToCanvas(entry.Screen) - GlobalPosition;
            var position = entry.Tag.GetTextPosition(point, textSize);
            position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
            CanvasDraw.Text(renderer, font, position + Vector2.One, entry.Tag.Text, size, new Color4(0f, 0f, 0f, 0.7f * entry.Tag.Color.A));
            CanvasDraw.Text(renderer, font, position, entry.Tag.Text, size, entry.Tag.Color);
        }
    }
}
