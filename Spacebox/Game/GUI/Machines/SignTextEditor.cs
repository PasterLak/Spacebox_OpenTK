using System.Numerics;
using ImGuiNET;
using Spacebox.Game.Signs;
using Color4 = OpenTK.Mathematics.Color4;

namespace Spacebox.Game.GUI;

public sealed class SignTextEditor
{
    private const float BlinkPeriod = 1.06f;

    private static readonly Vector4 SelectionColor = new Vector4(0.25f, 0.5f, 0.95f, 0.45f);

    private readonly Dictionary<char, string> _glyphs = new Dictionary<char, string>();
    private readonly Dictionary<char, float> _widths = new Dictionary<char, float>();
    private readonly List<float[]> _columns = new List<float[]>();
    private readonly List<float> _lineTops = new List<float>();
    private float _cachedFontSize;
    private float _lineHeight;
    private bool _dragging;
    private double _blinkStart;

    public bool Draw(SignDocument document, Vector2 size, Vector4 background, Color4 defaultColor, float scale, float margin)
    {
        var start = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##signEditor", size);
        bool hovered = ImGui.IsItemHovered();
        if (hovered) ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
        ImGui.SetNextFrameWantCaptureKeyboard(true);

        Layout(document, start, size, scale, margin);
        bool changed = HandleKeyboard(document);
        HandleMouse(document, hovered);
        if (changed) Layout(document, start, size, scale, margin);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(start, start + size, ImGui.ColorConvertFloat4ToU32(background));
        drawList.PushClipRect(start, start + size, true);
        DrawSelection(drawList, document);
        DrawText(drawList, document, defaultColor, scale);
        DrawCaret(drawList, document, defaultColor);
        drawList.PopClipRect();
        return changed;
    }

    public void ResetBlink()
    {
        _blinkStart = ImGui.GetTime();
        _dragging = false;
    }

    private void Layout(SignDocument document, Vector2 start, Vector2 size, float scale, float margin)
    {
        if (_cachedFontSize != ImGui.GetFontSize())
        {
            _cachedFontSize = ImGui.GetFontSize();
            _widths.Clear();
        }

        var lines = document.Lines;
        _lineHeight = ImGui.GetTextLineHeight() * scale;
        float top = start.Y + (size.Y - lines.Count * _lineHeight) * 0.5f;

        while (_columns.Count < lines.Count) _columns.Add(Array.Empty<float>());
        _lineTops.Clear();

        for (int l = 0; l < lines.Count; l++)
        {
            var chars = lines[l].Chars;
            if (_columns[l].Length != chars.Count + 1) _columns[l] = new float[chars.Count + 1];
            var columns = _columns[l];

            float width = 0f;
            for (int i = 0; i < chars.Count; i++) width += CharWidth(chars[i], scale);

            float x = lines[l].Align switch
            {
                SignAlign.Left => start.X + margin,
                SignAlign.Right => start.X + size.X - margin - width,
                _ => start.X + (size.X - width) * 0.5f
            };

            columns[0] = x;
            for (int i = 0; i < chars.Count; i++)
            {
                x += CharWidth(chars[i], scale);
                columns[i + 1] = x;
            }

            _lineTops.Add(top + l * _lineHeight);
        }
    }

    private bool HandleKeyboard(SignDocument document)
    {
        var io = ImGui.GetIO();
        bool ctrl = io.KeyCtrl;
        bool shift = io.KeyShift;
        bool changed = false;
        bool moved = false;

        if (ctrl && ImGui.IsKeyPressed(ImGuiKey.A, false)) { document.SelectAll(); moved = true; }
        if (ctrl && ImGui.IsKeyPressed(ImGuiKey.B, false)) document.ToggleBold();
        if (ctrl && (ImGui.IsKeyPressed(ImGuiKey.C, false) || ImGui.IsKeyPressed(ImGuiKey.X, false)) && document.HasSelection)
        {
            ImGui.SetClipboardText(document.SelectedText());
            if (ImGui.IsKeyPressed(ImGuiKey.X, false)) changed |= document.Delete();
        }
        if (ctrl && ImGui.IsKeyPressed(ImGuiKey.V, true)) changed |= document.Insert(ImGui.GetClipboardText() ?? "");

        if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, true)) { document.MoveLeft(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, true)) { document.MoveRight(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true)) { document.MoveUp(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true)) { document.MoveDown(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.Home, true)) { document.MoveHome(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.End, true)) { document.MoveEnd(shift); moved = true; }
        if (ImGui.IsKeyPressed(ImGuiKey.Backspace, true)) changed |= document.Backspace();
        if (ImGui.IsKeyPressed(ImGuiKey.Delete, true)) changed |= document.Delete();
        if (ImGui.IsKeyPressed(ImGuiKey.Enter, true) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter, true)) changed |= document.NewLine();

        if (!ctrl)
        {
            var queue = io.InputQueueCharacters;
            for (int i = 0; i < queue.Size; i++)
            {
                char c = (char)queue[i];
                if (c == '\r' || c == '\n') continue;
                changed |= document.Insert(c.ToString());
            }
        }

        if (changed || moved) _blinkStart = ImGui.GetTime();
        return changed;
    }

    private void HandleMouse(SignDocument document, bool hovered)
    {
        var mouse = ImGui.GetMousePos();
        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            document.SetCaret(HitTest(document, mouse), ImGui.GetIO().KeyShift);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) SelectWord(document);
            _dragging = true;
            _blinkStart = ImGui.GetTime();
        }
        else if (_dragging && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            document.SetCaret(HitTest(document, mouse), true);
        }
        else
        {
            _dragging = false;
        }
    }

    private SignCaret HitTest(SignDocument document, Vector2 mouse)
    {
        int line = _lineTops.Count - 1;
        for (int l = 0; l < _lineTops.Count; l++)
        {
            if (mouse.Y < _lineTops[l] + _lineHeight)
            {
                line = l;
                break;
            }
        }

        var columns = _columns[line];
        int count = document.Lines[line].Chars.Count;
        int column = count;
        for (int i = 0; i < count; i++)
        {
            if (mouse.X < (columns[i] + columns[i + 1]) * 0.5f)
            {
                column = i;
                break;
            }
        }
        return new SignCaret(line, column);
    }

    private static void SelectWord(SignDocument document)
    {
        var caret = document.Caret;
        var chars = document.Lines[caret.Line].Chars;
        int from = caret.Column;
        int to = caret.Column;
        while (from > 0 && !char.IsWhiteSpace(chars[from - 1].Value)) from--;
        while (to < chars.Count && !char.IsWhiteSpace(chars[to].Value)) to++;
        document.SetCaret(new SignCaret(caret.Line, from), false);
        document.SetCaret(new SignCaret(caret.Line, to), true);
    }

    private void DrawSelection(ImDrawListPtr drawList, SignDocument document)
    {
        if (!document.HasSelection) return;
        uint color = ImGui.ColorConvertFloat4ToU32(SelectionColor);
        var start = document.SelectionStart;
        var end = document.SelectionEnd;
        float newlineWidth = _lineHeight * 0.3f;

        for (int l = start.Line; l <= end.Line; l++)
        {
            var columns = _columns[l];
            int from = l == start.Line ? start.Column : 0;
            int to = l == end.Line ? end.Column : document.Lines[l].Chars.Count;
            float right = columns[to] + (l < end.Line ? newlineWidth : 0f);
            drawList.AddRectFilled(new Vector2(columns[from], _lineTops[l]), new Vector2(right, _lineTops[l] + _lineHeight), color);
        }
    }

    private void DrawText(ImDrawListPtr drawList, SignDocument document, Color4 defaultColor, float scale)
    {
        var font = ImGui.GetFont();
        float fontSize = ImGui.GetFontSize() * scale;
        float boldOffset = MathF.Max(1f, scale);

        for (int l = 0; l < document.Lines.Count; l++)
        {
            var chars = document.Lines[l].Chars;
            var columns = _columns[l];
            for (int i = 0; i < chars.Count; i++)
            {
                var c = chars[i];
                var tint = c.Color ?? defaultColor;
                uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(tint.R, tint.G, tint.B, 1f));
                var position = new Vector2(columns[i], _lineTops[l]);
                string glyph = Glyph(c.Value);
                drawList.AddText(font, fontSize, position, color, glyph);
                if (c.Bold) drawList.AddText(font, fontSize, position + new Vector2(boldOffset, 0f), color, glyph);
            }
        }
    }

    private void DrawCaret(ImDrawListPtr drawList, SignDocument document, Color4 defaultColor)
    {
        if ((ImGui.GetTime() - _blinkStart) % BlinkPeriod > BlinkPeriod * 0.5f) return;
        var caret = document.Caret;
        float x = _columns[caret.Line][caret.Column];
        float y = _lineTops[caret.Line];
        var tint = document.TypingColor ?? defaultColor;
        uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(tint.R, tint.G, tint.B, 1f));
        float thickness = document.TypingBold ? 3f : 1.5f;
        drawList.AddLine(new Vector2(x, y + 1f), new Vector2(x, y + _lineHeight - 1f), color, thickness);
    }

    private float CharWidth(SignChar c, float scale)
    {
        if (!_widths.TryGetValue(c.Value, out float width))
        {
            width = ImGui.CalcTextSize(Glyph(c.Value)).X;
            _widths[c.Value] = width;
        }
        return width * scale + (c.Bold ? MathF.Max(1f, scale) : 0f);
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
