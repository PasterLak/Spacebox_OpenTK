using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.GUI;

public enum OverlaySpanKind : byte
{
    Text,
    Section,
    Bar
}

public readonly struct OverlaySpan
{
    public readonly string Text;
    public readonly Color4 Color;
    public readonly OverlaySpanKind Kind;
    public readonly float Value;
    public readonly bool NewLine;

    public OverlaySpan(string text, Color4 color, OverlaySpanKind kind, float value, bool newLine)
    {
        Text = text ?? "";
        Color = color;
        Kind = kind;
        Value = value;
        NewLine = newLine;
    }
}

public sealed class OverlayText
{
    private readonly List<OverlaySpan> _spans = new List<OverlaySpan>(64);
    private bool _lineOpen;

    public IReadOnlyList<OverlaySpan> Spans => _spans;
    public float Elapsed { get; set; }
    public int Version { get; private set; }

    public void Clear()
    {
        _spans.Clear();
        _lineOpen = false;
        Version++;
    }

    public void Section(string title)
    {
        _spans.Add(new OverlaySpan(title, DarkTheme.Accent, OverlaySpanKind.Section, 0f, true));
        _lineOpen = false;
    }

    public void Line(string text) => Line(text, DarkTheme.Text);

    public void Line(string text, Color4 color)
    {
        _spans.Add(new OverlaySpan(text, color, OverlaySpanKind.Text, 0f, true));
        _lineOpen = true;
    }

    public void Append(string text) => Append(text, DarkTheme.Text);

    public void Append(string text, Color4 color)
    {
        _spans.Add(new OverlaySpan(text, color, OverlaySpanKind.Text, 0f, !_lineOpen));
        _lineOpen = true;
    }

    public void Bar(float fraction, string text = null)
    {
        _spans.Add(new OverlaySpan(text, DarkTheme.Text, OverlaySpanKind.Bar, Math.Clamp(fraction, 0f, 1f), !_lineOpen));
        _lineOpen = true;
    }
}
