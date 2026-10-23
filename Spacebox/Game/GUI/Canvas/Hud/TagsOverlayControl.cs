using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Hud;

public sealed class TagsOverlayControl : HudOverlayControl
{
    private readonly List<TagManager.ScreenTag> _tags = new List<TagManager.ScreenTag>(256);
    private readonly Dictionary<Tag, (Vector2 Position, int Frame)> _smoothed = new Dictionary<Tag, (Vector2, int)>();
    private readonly List<Tag> _stale = new List<Tag>();
    private float _viewportHeight = -1f;
    private int _frame;

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

        _frame++;
        TagManager.Instance.CollectScreenTags(_tags);
        for (int i = 0; i < _tags.Count; i++)
        {
            var entry = _tags[i];
            float size = MathF.Round(Tag.CalculateFontSize(entry.DistanceSquared));
            var textSize = font.Measure(entry.Tag.Text, size);
            var point = Smooth(entry.Tag, Root.ScreenToCanvas(entry.Screen) - GlobalPosition);
            var position = entry.Tag.GetTextPosition(point, textSize);
            position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
            CanvasDraw.Text(renderer, font, position + Vector2.One, entry.Tag.Text, size, new Color4(0f, 0f, 0f, 0.7f * entry.Tag.Color.A));
            CanvasDraw.Text(renderer, font, position, entry.Tag.Text, size, entry.Tag.Color);
        }

        RemoveStale();
    }

    private Vector2 Smooth(Tag tag, Vector2 target)
    {
        var position = _smoothed.TryGetValue(tag, out var previous) && previous.Frame == _frame - 1 ? TagScreenSmoothing.Next(previous.Position, target) : target;
        _smoothed[tag] = (position, _frame);
        return position;
    }

    private void RemoveStale()
    {
        if (_smoothed.Count <= _tags.Count) return;
        _stale.Clear();
        foreach (var pair in _smoothed)
            if (pair.Value.Frame != _frame) _stale.Add(pair.Key);
        foreach (var tag in _stale) _smoothed.Remove(tag);
    }
}
