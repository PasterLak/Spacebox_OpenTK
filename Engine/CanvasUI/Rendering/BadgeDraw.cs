using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public static class BadgeDraw
{
    private const float Padding = 3f;
    private const float Gap = 3f;
    private const float TrailingGap = 2f;

    public static float FontSize(float fontSize, float scale) => MathF.Max(6f, MathF.Round(fontSize * scale));

    public static float Width(CanvasFont font, IReadOnlyList<ListBadge> badges, float size)
    {
        if (badges == null || badges.Count == 0) return 0f;
        float width = 0f;
        for (int i = 0; i < badges.Count; i++) width += BadgeWidth(font, badges[i], size) + Gap;
        return width + TrailingGap;
    }

    public static float Draw(ICanvasRenderer renderer, CanvasFont font, IReadOnlyList<ListBadge> badges, float x, float y, float rowHeight, float size)
    {
        if (badges == null || badges.Count == 0) return x;
        float height = MathF.Floor(font.GetLineHeight(size) + 2f);
        float by = MathF.Floor(y + (rowHeight - height) * 0.5f);
        for (int i = 0; i < badges.Count; i++)
        {
            var badge = badges[i];
            float width = BadgeWidth(font, badge, size);
            renderer.DrawRect(new Rect2(x, by, width, height), badge.Color);
            CanvasDraw.Text(renderer, font, new Vector2(x + Padding, by + 1f), badge.Text, size, badge.TextColor);
            x += width + Gap;
        }
        return x + TrailingGap;
    }

    private static float BadgeWidth(CanvasFont font, ListBadge badge, float size) => MathF.Ceiling(font.MeasureWidth(badge.Text, size) + Padding * 2f);
}
