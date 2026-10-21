using System.Numerics;
using System.Text;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Signs;

namespace Spacebox.Game.GUI;

public static class SignUI
{
    private const string WindowId = "sign";

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 HintColor = new Vector4(0.22f, 0.24f, 0.29f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(0.75f, 0.25f, 0.05f, 1f);
    private static readonly Vector4 LightPreviewBackground = new Vector4(0.78f, 0.79f, 0.81f, 1f);
    private static readonly Vector4 DarkPreviewBackground = new Vector4(0.13f, 0.15f, 0.19f, 1f);
    private static readonly string[] QuickColors = { "white", "black", "red", "orange", "gold", "lime", "cyan", "blue", "purple", "gray" };

    private static readonly unsafe ImGuiInputTextCallback _callback = OnInputCallback;

    private static SignBlock _sign;
    private static string _text = "";
    private static string _blockName = "Sign";
    private static int _selectionStart;
    private static int _selectionEnd;
    private static bool _focusInput;

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _sign != null;

    public static void Open(SignBlock sign)
    {
        _sign = sign;
        _text = sign.Text;
        _blockName = GameAssets.GetBlockDataById(sign.Id)?.Name ?? _blockName;
        _selectionStart = _selectionEnd = Encoding.UTF8.GetByteCount(_text);
        _focusInput = true;
        UIManager.Open(WindowId);
    }

    public static void OnGUI()
    {
        if (!UIManager.IsOpen(WindowId) || _sign == null) return;

        if (_sign.Entity == null)
        {
            Close();
            return;
        }

        bool typing = ImGui.GetIO().WantTextInput;
        if (Input.IsKeyDown(Keys.Escape) || (!typing && Input.IsActionDown("inventory")))
        {
            Close();
            return;
        }

        var data = _sign.Data;
        var displaySize = ImGui.GetIO().DisplaySize;
        float windowWidth = displaySize.Y * 0.62f * MathF.Max(1f, data.Width * 0.8f);
        float windowHeight = displaySize.Y * 0.56f;
        float padding = windowHeight / 22f;
        float contentWidth = windowWidth - padding * 2f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("SignEditor", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration |
                                  ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _blockName);

        ImGui.SetCursorPosX(padding);
        DrawToolbar();

        ImGui.SetCursorPosX(padding);
        ImGui.PushTextWrapPos(padding + contentWidth);
        ImGui.TextColored(HintColor, "Select text and press B or a colour to wrap it. Tags: <b>bold</b>, <color=red>text</color>, <color=#ff8800>, and <left> <center> <right> at the start of a line.");
        ImGui.PopTextWrapPos();

        ImGui.SetCursorPosX(padding);
        if (_focusInput)
        {
            ImGui.SetKeyboardFocusHere();
            _focusInput = false;
        }
        uint maxBytes = (uint)(data.MaxLines * (data.MaxLineLength * 4 + 64));
        ImGui.InputTextMultiline("##signText", ref _text, maxBytes, new Vector2(contentWidth, windowHeight * 0.26f),
            ImGuiInputTextFlags.CallbackAlways, _callback);

        ImGui.SetCursorPosX(padding);
        DrawLimits(data);

        ImGui.SetCursorPosX(padding);
        DrawPreview(contentWidth, windowHeight * 0.3f);

        ImGui.SetCursorPosX(padding);
        float buttonWidth = (contentWidth - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
        if (ImGui.Button("Save", new Vector2(buttonWidth, 0f)))
        {
            Spacebox.Network.SignNetwork.SetText(_sign, _text);
            Close();
            ImGui.End();
            return;
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0f)))
        {
            Close();
            ImGui.End();
            return;
        }

        ImGui.End();
    }

    public static void Reset()
    {
        _sign = null;
        _text = "";
    }

    private static void DrawToolbar()
    {
        if (ImGui.Button("B")) Wrap("<b>", "</b>");
        TooltipFor("Bold");

        foreach (var name in QuickColors)
        {
            ImGui.SameLine();
            if (!SignMarkup.TryParseColor(name, out var color)) continue;
            var swatch = new Vector4(color.R, color.G, color.B, 1f);
            ImGui.PushStyleColor(ImGuiCol.Button, swatch);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, swatch);
            if (ImGui.Button("##color" + name, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()))) Wrap($"<color={name}>", "</color>");
            ImGui.PopStyleColor(2);
            TooltipFor(name);
        }

        ImGui.SameLine();
        if (ImGui.Button("Left")) Insert("<left>");
        ImGui.SameLine();
        if (ImGui.Button("Center")) Insert("<center>");
        ImGui.SameLine();
        if (ImGui.Button("Right")) Insert("<right>");
    }

    private static void DrawLimits(Spacebox.Game.Resource.SignBlockData data)
    {
        var lines = _text.Replace("\r", "").Split('\n');
        int longest = 0;
        foreach (var line in lines) longest = Math.Max(longest, SignMarkup.VisibleLength(line));

        string info = $"Lines {lines.Length}/{data.MaxLines}   longest line {longest}/{data.MaxLineLength}";
        if (lines.Length > data.MaxLines || longest > data.MaxLineLength)
            ImGui.TextColored(WarningColor, info + "   (extra text is cut when saving)");
        else
            ImGui.TextColored(HintColor, info);
    }

    private static void DrawPreview(float width, float height)
    {
        var data = _sign.Data;
        var start = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        float luminance = data.TextColor.R * 0.3f + data.TextColor.G * 0.59f + data.TextColor.B * 0.11f;
        var background = luminance > 0.5f ? DarkPreviewBackground : LightPreviewBackground;
        drawList.AddRectFilled(start, start + new Vector2(width, height), ImGui.ColorConvertFloat4ToU32(background));

        var lines = SignMarkup.Parse(data.SanitizeText(_text), data.TextColor, data.Align);
        float lineHeight = ImGui.GetTextLineHeight();
        float top = start.Y + (height - lines.Count * lineHeight) * 0.5f;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            float lineWidth = 0f;
            foreach (var run in line.Runs) lineWidth += ImGui.CalcTextSize(run.Text).X + (run.Bold ? 1f : 0f);

            float x = line.Align switch
            {
                SignAlign.Left => start.X + 6f,
                SignAlign.Right => start.X + width - 6f - lineWidth,
                _ => start.X + (width - lineWidth) * 0.5f
            };
            float y = top + i * lineHeight;

            foreach (var run in line.Runs)
            {
                uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(run.Color.R, run.Color.G, run.Color.B, 1f));
                drawList.AddText(new Vector2(x, y), color, run.Text);
                if (run.Bold) drawList.AddText(new Vector2(x + 1f, y), color, run.Text);
                x += ImGui.CalcTextSize(run.Text).X + (run.Bold ? 1f : 0f);
            }
        }

        ImGui.Dummy(new Vector2(width, height));
    }

    private static void Wrap(string open, string close)
    {
        var (from, to) = SelectionChars();
        _text = _text.Substring(0, from) + open + _text.Substring(from, to - from) + close + _text.Substring(to);
        int caret = Encoding.UTF8.GetByteCount(_text.Substring(0, to + open.Length));
        _selectionStart = _selectionEnd = caret;
        _focusInput = true;
    }

    private static void Insert(string tag)
    {
        var (from, _) = SelectionChars();
        int lineStart = _text.LastIndexOf('\n', Math.Max(0, from - 1));
        lineStart = from == 0 ? 0 : lineStart + 1;
        _text = _text.Substring(0, lineStart) + tag + _text.Substring(lineStart);
        _focusInput = true;
    }

    private static (int From, int To) SelectionChars()
    {
        int a = ByteToCharIndex(Math.Min(_selectionStart, _selectionEnd));
        int b = ByteToCharIndex(Math.Max(_selectionStart, _selectionEnd));
        return (a, b);
    }

    private static int ByteToCharIndex(int byteIndex)
    {
        var bytes = Encoding.UTF8.GetBytes(_text);
        byteIndex = Math.Clamp(byteIndex, 0, bytes.Length);
        return Encoding.UTF8.GetCharCount(bytes, 0, byteIndex);
    }

    private static unsafe int OnInputCallback(ImGuiInputTextCallbackData* raw)
    {
        var data = new ImGuiInputTextCallbackDataPtr(raw);
        _selectionStart = data.SelectionStart;
        _selectionEnd = data.SelectionEnd;
        if (_selectionStart == _selectionEnd) _selectionStart = _selectionEnd = data.CursorPos;
        return 0;
    }

    private static void TooltipFor(string text)
    {
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }

    private static void Close()
    {
        _sign = null;
        UIManager.CloseAll();
    }
}
