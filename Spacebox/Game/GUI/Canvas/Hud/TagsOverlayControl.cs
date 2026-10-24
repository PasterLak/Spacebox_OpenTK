using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class TagsOverlayControl : HudOverlayControl
{
    private readonly List<TagManager.ScreenTag> _tags = new List<TagManager.ScreenTag>(256);
    private float _viewportHeight = -1f;

    protected override bool IsShown() => Settings.ShowInterface && GameScene.Current?.Tags != null;

    protected override void DrawOverlay(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        if (Root == null || font == null) return;
        if (_viewportHeight != Root.ViewportSize.Y)
        {
            _viewportHeight = Root.ViewportSize.Y;
            Tag.SetFontSizes(Root.ViewportSize);
        }

        float scale = MathF.Max(0.01f, Root.Scale);
        var origin = GlobalPosition;
        GameScene.Current.Tags.CollectScreenTags(_tags);
        for (int i = 0; i < _tags.Count; i++)
        {
            var entry = _tags[i];
            float size = MathF.Round(Tag.CalculateFontSize(entry.DistanceSquared));
            var textSize = font.Measure(entry.Tag.Text, size);
            var screen = entry.Tag.GetTextPosition(entry.Screen, textSize * scale);
            var position = new Vector2(MathF.Round(screen.X), MathF.Round(screen.Y));
            var local = Root.ScreenToCanvas(position) - origin;
            float shadow = 1f / scale;
            CanvasDraw.Text(renderer, font, local + new Vector2(shadow), entry.Tag.Text, size, new Color4(0f, 0f, 0f, 0.7f * entry.Tag.Color.A));
            CanvasDraw.Text(renderer, font, local, entry.Tag.Text, size, entry.Tag.Color);
        }
    }
}
