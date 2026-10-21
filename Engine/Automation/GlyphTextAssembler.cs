using System.Text;

namespace Engine.Automation;

public readonly record struct GlyphPlacement(char Char, float PenX, float LineTop, float Advance, float LineHeight, float SpaceAdvance);

public sealed class GlyphTextAssembler
{
    private const float SameLineTolerance = 0.3f;
    private const float JoinGapInSpaces = 0.5f;
    private const float MaxGapInSpaces = 3.5f;
    private const float DuplicateDistance = 3f;

    private readonly List<UiTextRun> _runs = new List<UiTextRun>();
    private readonly StringBuilder _text = new StringBuilder();
    private bool _hasRun;
    private float _runX;
    private float _runTop;
    private float _runHeight;
    private GlyphPlacement _last;

    public void Add(GlyphPlacement glyph)
    {
        if (_hasRun && TryAppend(glyph)) return;

        Flush();
        _hasRun = true;
        _runX = glyph.PenX;
        _runTop = glyph.LineTop;
        _runHeight = glyph.LineHeight;
        _text.Append(glyph.Char);
        _last = glyph;
    }

    public List<UiTextRun> Finish()
    {
        Flush();
        var result = new List<UiTextRun>(_runs);
        _runs.Clear();
        return result;
    }

    private bool TryAppend(GlyphPlacement glyph)
    {
        float tolerance = MathF.Max(_last.LineHeight, glyph.LineHeight) * SameLineTolerance;
        if (MathF.Abs(glyph.LineTop - _last.LineTop) > tolerance) return false;

        float space = glyph.SpaceAdvance > 0f ? glyph.SpaceAdvance : glyph.LineHeight * 0.5f;
        float gap = glyph.PenX - (_last.PenX + _last.Advance);
        if (gap < -space || gap > space * MaxGapInSpaces) return false;

        if (gap >= space * JoinGapInSpaces) _text.Append(' ', Math.Max(1, (int)MathF.Round(gap / space)));

        _text.Append(glyph.Char);
        _runHeight = MathF.Max(_runHeight, glyph.LineHeight);
        _last = glyph;
        return true;
    }

    private void Flush()
    {
        if (!_hasRun) return;

        var run = new UiTextRun(_text.ToString(), _runX, _runTop, _last.PenX + _last.Advance - _runX, _runHeight);
        if (!IsDuplicate(run)) _runs.Add(run);

        _text.Clear();
        _hasRun = false;
    }

    private bool IsDuplicate(UiTextRun run)
    {
        for (int i = _runs.Count - 1; i >= 0; i--)
        {
            var other = _runs[i];
            if (other.Text == run.Text && MathF.Abs(other.X - run.X) <= DuplicateDistance && MathF.Abs(other.Y - run.Y) <= DuplicateDistance)
                return true;
        }

        return false;
    }
}
