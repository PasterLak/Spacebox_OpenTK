using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Signs;
using Color4 = OpenTK.Mathematics.Color4;

namespace Spacebox.Game.GUI;

public sealed class SignUI : IGameUiController
{
    private const string WindowId = "sign";
    private const string MeasureGlyph = "M";

    public static readonly string[] QuickColors = { "white", "black", "red", "orange", "gold", "lime", "cyan", "blue", "purple", "gray" };
    public static readonly SignAlign[] Alignments = { SignAlign.Left, SignAlign.Center, SignAlign.Right };

    private static readonly Color4 LightBackground = new Color4(0.78f, 0.79f, 0.81f, 1f);
    private static readonly Color4 DarkBackground = new Color4(0.13f, 0.15f, 0.19f, 1f);

    private SignBlock _sign;
    private SignDocument _document;
    private string _blockName = "Sign";

    public bool IsOpen => UIManager.IsOpen(WindowId) && _sign != null;
    public SignBlock Sign => _sign;
    public SignDocument Document => _document;
    public string BlockName => _blockName;
    public float SignGlyphWidth { get; private set; }
    public float SignLineHeight { get; private set; }

    public void Open(SignBlock sign)
    {
        var data = sign.Data;
        _sign = sign;
        _document = SignDocument.FromMarkup(sign.Text, data.MaxLines, data.MaxLineLength, data.Align);
        _blockName = GameAssets.GetBlockDataById(sign.Id)?.Name ?? _blockName;
        SignGlyphWidth = SignTextBaker.MeasureText(MeasureGlyph, data.Font);
        SignLineHeight = SignTextBaker.BaseLineHeight(data.Font);
        UIManager.Open(WindowId);
    }

    public void OnGUI()
    {
        if (!IsOpen) return;
        if (_sign.Entity == null || Input.IsKeyDown(Keys.Escape)) Close();
    }

    public float FitScale()
    {
        var data = _sign.Data;
        var lines = SignMarkup.Parse(_document.ToMarkup(), data.TextColor, data.Align);
        return SignTextBaker.FitScale(lines, data.PixelsPerBlock * data.Width, data.PixelsPerBlock, data.Margin, data.Font);
    }

    public static float Largest(float fit) => Math.Clamp(fit, SignTextSize.MinScale, SignTextSize.MaxScale);

    public SignTextSize ClampTextSize(float fit)
    {
        var size = _document.TextSize;
        float largest = Largest(fit);
        if (!size.Fill && size.Scale > largest) _document.TextSize = size = new SignTextSize(largest, false);
        return size;
    }

    public void ToggleFill(float fit)
    {
        var size = _document.TextSize;
        _document.TextSize = new SignTextSize(size.Fill ? Largest(fit) : size.Scale, !size.Fill);
    }

    public void SetScale(float value, float fit)
    {
        _document.TextSize = new SignTextSize(MathF.Min(value, Largest(fit)), false);
    }

    public static Color4 BackgroundFor(Color4 textColor)
    {
        float luminance = textColor.R * 0.3f + textColor.G * 0.59f + textColor.B * 0.11f;
        return luminance > 0.5f ? DarkBackground : LightBackground;
    }

    public void Save()
    {
        if (_sign != null) Spacebox.Network.SignNetwork.SetText(_sign, _document.ToMarkup());
        Close();
    }

    public void Reset()
    {
        _sign = null;
        _document = null;
    }

    public void Close()
    {
        _sign = null;
        _document = null;
        UIManager.CloseAll();
    }
}
