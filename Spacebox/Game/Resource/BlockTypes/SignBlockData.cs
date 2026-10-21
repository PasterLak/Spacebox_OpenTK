using System.Text;
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Signs;

namespace Spacebox.Game.Resource;

public class SignBlockData : BlockData, IBlockTooltip
{
    public const int MaxWidth = 4;
    public const int MaxLineLengthLimit = 256;
    public const string DefaultFont = "Resources/Fonts/FusionPixel/FusionPixel12pxMonospacedLatin.ttf";

    public int Width { get; }
    public int MaxLines { get; }
    public int MaxLineLength { get; }
    public string DefaultText { get; }
    public Color4 TextColor { get; }
    public SignAlign Align { get; }
    public int PixelsPerBlock { get; }
    public int Margin { get; }
    public bool Glow { get; }
    public float ViewDistance { get; }
    public string Font { get; }

    public SignBlockData(BlockData baseBlock, SignJSON settings) : base(baseBlock)
    {
        settings ??= new SignJSON();

        Width = Math.Clamp((int)Size.X, 1, MaxWidth);
        MaxLines = Math.Clamp(settings.MaxLines, 1, 16);
        MaxLineLength = Math.Clamp(settings.MaxLineLength, 1, MaxLineLengthLimit);
        PixelsPerBlock = Math.Clamp(settings.PixelsPerBlock, 32, 256);
        Margin = Math.Clamp(settings.Margin, 0, PixelsPerBlock / 4);
        Glow = settings.Glow;
        ViewDistance = Math.Clamp(settings.ViewDistance, 4f, 256f);
        Font = string.IsNullOrWhiteSpace(settings.Font) ? DefaultFont : settings.Font.Trim();

        if (!SignMarkup.TryParseColor(settings.TextColor, out var color))
        {
            Debug.Error($"[SignBlockData] Unknown text color <{settings.TextColor}> for <{baseBlock.Name}>, using black");
            color = new Color4(0.05f, 0.05f, 0.05f, 1f);
        }
        TextColor = color;

        if (!SignMarkup.TryParseAlign(settings.Align, out var align))
        {
            Debug.Error($"[SignBlockData] Unknown text alignment <{settings.Align}> for <{baseBlock.Name}>, using center");
            align = SignAlign.Center;
        }
        Align = align;

        DefaultText = SignMarkup.Sanitize(settings.DefaultText, MaxLines, MaxLineLength);
    }

    public string SanitizeText(string text) => SignMarkup.Sanitize(text, MaxLines, MaxLineLength);

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nText: up to {MaxLines} lines of {MaxLineLength} characters");
    }
}
