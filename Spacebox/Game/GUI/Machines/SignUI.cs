using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Signs;
using Color4 = OpenTK.Mathematics.Color4;

namespace Spacebox.Game.GUI;

public static class SignUI
{
    private const string WindowId = "sign";
    private const float MinTextScale = 0.5f;
    private const float MaxTextScale = 4f;
    private const string MeasureGlyph = "M";

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 HintColor = new Vector4(0.22f, 0.24f, 0.29f, 1f);
    private static readonly Vector4 ActiveButtonColor = new Vector4(0.85f, 0.5f, 0.05f, 1f);
    private static readonly Vector4 LightBackground = new Vector4(0.78f, 0.79f, 0.81f, 1f);
    private static readonly Vector4 DarkBackground = new Vector4(0.13f, 0.15f, 0.19f, 1f);
    private static readonly string[] QuickColors = { "white", "black", "red", "orange", "gold", "lime", "cyan", "blue", "purple", "gray" };
    private static readonly SignAlign[] Alignments = { SignAlign.Left, SignAlign.Center, SignAlign.Right };

    private static readonly SignTextEditor _editor = new SignTextEditor();

    private static SignBlock _sign;
    private static SignDocument _document;
    private static string _blockName = "Sign";
    private static float _signGlyphWidth;
    private static float _signLineHeight;

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _sign != null;

    public static void Open(SignBlock sign)
    {
        var data = sign.Data;
        _sign = sign;
        _document = SignDocument.FromMarkup(sign.Text, data.MaxLines, data.MaxLineLength, data.Align);
        _blockName = GameAssets.GetBlockDataById(sign.Id)?.Name ?? _blockName;
        _signGlyphWidth = SignTextBaker.MeasureText(MeasureGlyph, data.Font);
        _signLineHeight = SignTextBaker.BaseLineHeight(data.Font);
        _editor.ResetBlink();
        UIManager.Open(WindowId);
    }

    public static void OnGUI()
    {
        if (!UIManager.IsOpen(WindowId) || _sign == null) return;

        if (_sign.Entity == null || Input.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        var data = _sign.Data;
        var displaySize = ImGui.GetIO().DisplaySize;
        float editorHeight = displaySize.Y * 0.3f;
        float windowWidth = MathF.Max(displaySize.Y * 0.62f, editorHeight * data.Width + displaySize.Y * 0.08f);
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
        DrawStyleRow(data.TextColor);
        ImGui.SetCursorPosX(padding);
        DrawLayoutRow();

        float fit = FitScale(data);
        ImGui.SetCursorPosX(padding);
        DrawSizeRow(fit, contentWidth);

        float editorWidth = MathF.Min(contentWidth, editorHeight * data.Width);
        ImGui.SetCursorPosX(padding + (contentWidth - editorWidth) * 0.5f);
        float signToEditor = editorWidth / (data.PixelsPerBlock * data.Width);
        float textScale = _document.TextSize.Resolve(fit);
        float glyphScale = Math.Clamp(_signGlyphWidth * signToEditor / ImGui.CalcTextSize(MeasureGlyph).X, MinTextScale, MaxTextScale) * textScale;
        var metrics = new SignEditorMetrics(glyphScale, _signLineHeight * textScale * signToEditor, data.Margin * signToEditor);
        _editor.Draw(_document, new Vector2(editorWidth, editorHeight), BackgroundFor(data.TextColor), data.TextColor, metrics);

        ImGui.SetCursorPosX(padding);
        var caret = _document.Caret;
        ImGui.TextColored(HintColor, $"Line {caret.Line + 1}/{data.MaxLines}   {_document.Lines[caret.Line].Chars.Count}/{data.MaxLineLength} characters");

        ImGui.SetCursorPos(new Vector2(padding, windowHeight - padding - ImGui.GetFrameHeight()));
        float buttonWidth = (contentWidth - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
        if (ImGui.Button("Save", new Vector2(buttonWidth, 0f)))
        {
            Spacebox.Network.SignNetwork.SetText(_sign, _document.ToMarkup());
            Close();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0f))) Close();

        ImGui.End();
    }

    public static void Reset()
    {
        _sign = null;
        _document = null;
    }

    private static void DrawStyleRow(Color4 defaultColor)
    {
        if (ToggleButton("B", _document.TypingBold)) _document.ToggleBold();
        TooltipFor("Bold (Ctrl+B)");

        ImGui.SameLine();
        if (ColorSwatch("default", defaultColor, !_document.TypingColor.HasValue)) _document.ApplyColor(null);
        TooltipFor("Sign colour");

        foreach (var name in QuickColors)
        {
            if (!SignMarkup.TryParseColor(name, out var color)) continue;
            ImGui.SameLine();
            bool selected = _document.TypingColor.HasValue && _document.TypingColor.Value == color;
            if (ColorSwatch(name, color, selected)) _document.ApplyColor(color);
            TooltipFor(name);
        }

    }

    private static void DrawLayoutRow()
    {
        for (int i = 0; i < Alignments.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            if (ToggleButton(Alignments[i].ToString(), _document.CurrentAlign == Alignments[i])) _document.SetAlign(Alignments[i]);
        }

        ImGui.SameLine();
        if (ImGui.Button("Reset style")) _document.ResetStyle();
        TooltipFor("Removes bold, colours and alignment from the selection, or every style and the text size when nothing is selected");
    }

    private static float FitScale(Spacebox.Game.Resource.SignBlockData data)
    {
        var lines = SignMarkup.Parse(_document.ToMarkup(), data.TextColor, data.Align);
        return SignTextBaker.FitScale(lines, data.PixelsPerBlock * data.Width, data.PixelsPerBlock, data.Margin, data.Font);
    }

    private static void DrawSizeRow(float fit, float width)
    {
        var size = _document.TextSize;
        float largest = Math.Clamp(fit, SignTextSize.MinScale, SignTextSize.MaxScale);
        if (!size.Fill && size.Scale > largest) _document.TextSize = size = new SignTextSize(largest, false);

        if (ToggleButton("Fill", size.Fill)) _document.TextSize = size = new SignTextSize(size.Fill ? largest : size.Scale, !size.Fill);
        TooltipFor("Makes the text as big as the sign allows; use new lines to fit long text better");

        ImGui.SameLine();
        ImGui.TextColored(HintColor, "Size");
        ImGui.SameLine();
        bool locked = size.Fill || largest <= SignTextSize.MinScale;
        float value = size.Fill ? largest : size.Scale;
        ImGui.BeginDisabled(locked);
        ImGui.SetNextItemWidth(width * 0.45f);
        if (ImGui.SliderFloat("##signTextSize", ref value, SignTextSize.MinScale, MathF.Max(largest, SignTextSize.MinScale + 0.1f), "%.1fx"))
            _document.TextSize = new SignTextSize(MathF.Min(value, largest), false);
        ImGui.EndDisabled();
        TooltipFor(largest <= SignTextSize.MinScale ? "The text does not fit bigger; shorten the lines or split them" : "Text size for the whole sign");
    }

    private static bool ToggleButton(string label, bool active)
    {
        bool pressed = ImGui.Button(label);
        if (active) Outline(ActiveButtonColor);
        return pressed;
    }

    private static bool ColorSwatch(string name, Color4 color, bool selected)
    {
        var swatch = new Vector4(color.R, color.G, color.B, 1f);
        ImGui.PushStyleColor(ImGuiCol.Button, swatch);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, swatch);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, swatch);
        float size = ImGui.GetFrameHeight();
        bool pressed = ImGui.Button("##color" + name, new Vector2(size, size));
        ImGui.PopStyleColor(3);
        if (selected) Outline(ActiveButtonColor);
        return pressed;
    }

    private static void Outline(Vector4 color)
    {
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - Vector2.One, ImGui.GetItemRectMax() + Vector2.One, ImGui.ColorConvertFloat4ToU32(color), 0f, ImDrawFlags.None, 2f);
    }

    private static Vector4 BackgroundFor(Color4 textColor)
    {
        float luminance = textColor.R * 0.3f + textColor.G * 0.59f + textColor.B * 0.11f;
        return luminance > 0.5f ? DarkBackground : LightBackground;
    }

    private static void TooltipFor(string text)
    {
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }

    private static void Close()
    {
        _sign = null;
        _document = null;
        UIManager.CloseAll();
    }
}
