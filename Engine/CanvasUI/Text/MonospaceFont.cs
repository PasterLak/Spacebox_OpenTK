using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class MonospaceFont : CanvasFont
{
    private readonly float _baseSize;
    private readonly float _lineHeight;
    private readonly float _advance;

    public MonospaceFont(float baseSize = 12f, float advance = 6f, float lineHeight = 14f)
    {
        _baseSize = baseSize;
        _advance = advance;
        _lineHeight = lineHeight;
    }

    public override float BaseSize => _baseSize;
    public override float LineHeight => _lineHeight;
    public override Texture2D Atlas { get; } = new Texture2D();

    public override bool TryGetGlyph(char c, out CanvasGlyph glyph)
    {
        glyph = new CanvasGlyph(_advance, Vector2.Zero, new Vector2(_advance, _lineHeight), new Rect2(0f, 0f, 1f, 1f));
        return true;
    }
}
