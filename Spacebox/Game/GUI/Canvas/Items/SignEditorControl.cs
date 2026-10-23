using Engine;
using Engine.CanvasUI;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Signs;

namespace Spacebox.Game.GUI.Canvas.Items;

public sealed class SignEditorControl : Control
{
    private const float BlinkPeriod = 1.06f;
    private const float MinGlyphScale = 0.5f;
    private const float MaxGlyphScale = 4f;
    private const string MeasureGlyph = "M";
    private static readonly Color4 SelectionColor = new Color4(0.25f, 0.5f, 0.95f, 0.45f);

    private readonly Dictionary<char, string> _glyphs = new Dictionary<char, string>();
    private readonly List<float[]> _columns = new List<float[]>();
    private readonly List<float> _lineTops = new List<float>();
    private float _fontSize;
    private float _lineHeight;
    private float _margin;
    private bool _dragging;
    private double _blinkStart;

    public SignEditorControl()
    {
        FocusMode = FocusMode.All;
        MouseFilter = MouseFilter.Stop;
    }

    public SignDocument Document { get; set; }
    public Color4 DefaultColor { get; set; } = Color4.White;
    public Color4 Background { get; set; } = Color4.Gray;
    public float SignGlyphWidth { get; set; } = 6f;
    public float SignLineHeight { get; set; } = 12f;
    public float SignPixelsWide { get; set; } = 64f;
    public float SignMargin { get; set; }
    public float TextScale { get; set; } = 1f;
    public override bool IsInteractive => true;

    public void ResetBlink()
    {
        _blinkStart = Time.Total;
        _dragging = false;
    }

    protected override bool OnKeyPress(KeyPress press)
    {
        var document = Document;
        if (document == null) return false;

        bool changed = false;
        bool handled = true;
        bool shift = press.Shift;
        if (press.Control)
        {
            switch (press.Key)
            {
                case Keys.A: document.SelectAll(); break;
                case Keys.B: document.ToggleBold(); break;
                case Keys.C:
                    if (document.HasSelection) Root?.Clipboard?.SetText(document.SelectedText());
                    break;
                case Keys.X:
                    if (document.HasSelection)
                    {
                        Root?.Clipboard?.SetText(document.SelectedText());
                        changed = document.Delete();
                    }
                    break;
                case Keys.V: changed = document.Insert(Root?.Clipboard?.GetText() ?? ""); break;
                default: handled = false; break;
            }
        }
        else
        {
            switch (press.Key)
            {
                case Keys.Left: document.MoveLeft(shift); break;
                case Keys.Right: document.MoveRight(shift); break;
                case Keys.Up: document.MoveUp(shift); break;
                case Keys.Down: document.MoveDown(shift); break;
                case Keys.Home: document.MoveHome(shift); break;
                case Keys.End: document.MoveEnd(shift); break;
                case Keys.Backspace: changed = document.Backspace(); break;
                case Keys.Delete: changed = document.Delete(); break;
                case Keys.Enter:
                case Keys.KeyPadEnter: changed = document.NewLine(); break;
                default: handled = false; break;
            }
        }

        if (handled) _blinkStart = Time.Total;
        return handled || changed;
    }

    protected override void OnTextInput(string text)
    {
        if (Document == null || string.IsNullOrEmpty(text)) return;
        foreach (char c in text)
        {
            if (c == '\r' || c == '\n' || char.IsControl(c)) continue;
            Document.Insert(Glyph(c));
        }
        _blinkStart = Time.Total;
    }

    protected override bool OnMouseDown(Vector2 localPosition, MouseButton button)
    {
        if (button != MouseButton.Left || Document == null) return false;
        GrabFocus();
        Layout();
        Document.SetCaret(HitTest(localPosition), Input.IsKey(Keys.LeftShift) || Input.IsKey(Keys.RightShift));
        if (Root != null && Root.ClickCount >= 2) SelectWord();
        _dragging = true;
        _blinkStart = Time.Total;
        return true;
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        base.OnMouseMove(localPosition, pressed);
        if (!_dragging || !pressed || Document == null) return;
        Document.SetCaret(HitTest(localPosition), true);
    }

    protected override bool OnMouseUp(Vector2 localPosition, MouseButton button, bool inside)
    {
        if (button != MouseButton.Left) return false;
        _dragging = false;
        return true;
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, Background);
        if (Document == null || GetThemeFont() == null) return;

        Layout();
        renderer.PushClip(LocalRect.Shrink(_margin, _margin, _margin, _margin));
        DrawSelection(renderer);
        DrawText(renderer);
        DrawCaret(renderer);
        renderer.PopClip();
    }

    private void Layout()
    {
        var font = GetThemeFont();
        var lines = Document.Lines;
        float baseSize = GetThemeFontSize();
        float signToEditor = Size.X / MathF.Max(1f, SignPixelsWide);
        float glyph = MathF.Max(0.01f, font.MeasureWidth(MeasureGlyph, baseSize));
        float scale = Math.Clamp(SignGlyphWidth * signToEditor / glyph, MinGlyphScale, MaxGlyphScale) * TextScale;
        _fontSize = baseSize * scale;
        _lineHeight = SignLineHeight * TextScale * signToEditor;
        _margin = SignMargin * signToEditor;

        float widest = 0f;
        for (int l = 0; l < lines.Count; l++) widest = MathF.Max(widest, LineWidth(lines[l], _fontSize));
        float availableWidth = MathF.Max(1f, Size.X - _margin * 2f);
        float availableHeight = MathF.Max(1f, Size.Y - _margin * 2f);
        float shrink = MathF.Min(1f, availableHeight / MathF.Max(1f, lines.Count * _lineHeight));
        if (widest > 0f) shrink = MathF.Min(shrink, availableWidth / widest);
        _fontSize *= shrink;
        _lineHeight *= shrink;

        float top = (Size.Y - lines.Count * _lineHeight) * 0.5f;
        while (_columns.Count < lines.Count) _columns.Add(Array.Empty<float>());
        _lineTops.Clear();
        for (int l = 0; l < lines.Count; l++)
        {
            var chars = lines[l].Chars;
            if (_columns[l].Length != chars.Count + 1) _columns[l] = new float[chars.Count + 1];
            var columns = _columns[l];
            float width = LineWidth(lines[l], _fontSize);
            float x = lines[l].Align switch
            {
                SignAlign.Left => _margin,
                SignAlign.Right => Size.X - _margin - width,
                _ => (Size.X - width) * 0.5f
            };

            columns[0] = x;
            for (int i = 0; i < chars.Count; i++)
            {
                x += CharWidth(chars[i], _fontSize);
                columns[i + 1] = x;
            }
            _lineTops.Add(top + l * _lineHeight);
        }
    }

    private float LineWidth(SignDocumentLine line, float size)
    {
        float width = 0f;
        for (int i = 0; i < line.Chars.Count; i++) width += CharWidth(line.Chars[i], size);
        return width;
    }

    private float CharWidth(SignChar c, float size)
    {
        float width = GetThemeFont().MeasureWidth(Glyph(c.Value), size);
        return width + (c.Bold ? BoldOffset(size) : 0f);
    }

    private float BoldOffset(float size) => MathF.Max(1f, size / GetThemeFontSize());

    private SignCaret HitTest(Vector2 local)
    {
        int line = _lineTops.Count - 1;
        for (int l = 0; l < _lineTops.Count; l++)
        {
            if (local.Y < _lineTops[l] + _lineHeight)
            {
                line = l;
                break;
            }
        }
        if (line < 0) return new SignCaret(0, 0);

        var columns = _columns[line];
        int count = Document.Lines[line].Chars.Count;
        int column = count;
        for (int i = 0; i < count; i++)
        {
            if (local.X < (columns[i] + columns[i + 1]) * 0.5f)
            {
                column = i;
                break;
            }
        }
        return new SignCaret(line, column);
    }

    private void SelectWord()
    {
        var caret = Document.Caret;
        var chars = Document.Lines[caret.Line].Chars;
        int from = caret.Column;
        int to = caret.Column;
        while (from > 0 && !char.IsWhiteSpace(chars[from - 1].Value)) from--;
        while (to < chars.Count && !char.IsWhiteSpace(chars[to].Value)) to++;
        Document.SetCaret(new SignCaret(caret.Line, from), false);
        Document.SetCaret(new SignCaret(caret.Line, to), true);
    }

    private void DrawSelection(ICanvasRenderer renderer)
    {
        if (!Document.HasSelection) return;
        var start = Document.SelectionStart;
        var end = Document.SelectionEnd;
        float newlineWidth = _lineHeight * 0.3f;
        for (int l = start.Line; l <= end.Line; l++)
        {
            var columns = _columns[l];
            int from = l == start.Line ? start.Column : 0;
            int to = l == end.Line ? end.Column : Document.Lines[l].Chars.Count;
            float right = columns[to] + (l < end.Line ? newlineWidth : 0f);
            renderer.DrawRect(new Rect2(columns[from], _lineTops[l], right - columns[from], _lineHeight), SelectionColor);
        }
    }

    private void DrawText(ICanvasRenderer renderer)
    {
        var font = GetThemeFont();
        float lift = (_lineHeight - font.GetLineHeight(_fontSize)) * 0.5f;
        float bold = BoldOffset(_fontSize);
        for (int l = 0; l < Document.Lines.Count; l++)
        {
            var chars = Document.Lines[l].Chars;
            var columns = _columns[l];
            for (int i = 0; i < chars.Count; i++)
            {
                var c = chars[i];
                var tint = c.Color ?? DefaultColor;
                var color = new Color4(tint.R, tint.G, tint.B, 1f);
                var position = new Vector2(columns[i], _lineTops[l] + lift);
                string glyph = Glyph(c.Value);
                CanvasDraw.Text(renderer, font, position, glyph, _fontSize, color);
                if (c.Bold) CanvasDraw.Text(renderer, font, position + new Vector2(bold, 0f), glyph, _fontSize, color);
            }
        }
    }

    private void DrawCaret(ICanvasRenderer renderer)
    {
        if (!HasFocus || (Time.Total - _blinkStart) % BlinkPeriod > BlinkPeriod * 0.5f) return;
        var caret = Document.Caret;
        float x = _columns[caret.Line][caret.Column];
        float y = _lineTops[caret.Line];
        var tint = Document.TypingColor ?? DefaultColor;
        float thickness = Document.TypingBold ? 3f : 2f;
        renderer.DrawRect(new Rect2(MathF.Round(x - thickness * 0.5f), y + 1f, thickness, MathF.Max(1f, _lineHeight - 2f)), new Color4(tint.R, tint.G, tint.B, 1f));
    }

    private string Glyph(char value)
    {
        if (!_glyphs.TryGetValue(value, out var glyph))
        {
            glyph = value.ToString();
            _glyphs[value] = glyph;
        }
        return glyph;
    }
}
