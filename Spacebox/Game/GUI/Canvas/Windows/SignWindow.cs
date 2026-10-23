using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.Signs;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class SignWindow : GameWindow
{
    private const float EditorHeight = 170f;
    private const float MinWidth = 360f;
    private const float SwatchSize = 20f;

    private readonly Button _bold = new Button { Text = "B", ThemeTypeVariation = "SmallButton", ToggleMode = true, TooltipText = "Bold (Ctrl+B)" };
    private readonly ColorSwatch[] _swatches;
    private readonly ColorSwatch _defaultSwatch = new ColorSwatch(Color4.White, SwatchSize, "Sign colour");
    private readonly HBoxContainer _colors = new HBoxContainer();
    private readonly Button[] _alignButtons = new Button[SignUI.Alignments.Length];
    private readonly Button _fill = new Button { Text = "Fill", ThemeTypeVariation = "SmallButton", ToggleMode = true, TooltipText = "Makes the text as big as the sign allows; use new lines to fit long text better" };
    private readonly HSlider _size = new HSlider { Step = 0.1, MinValue = SignTextSize.MinScale, CustomMinimumSize = new Vector2(160f, 0f) };
    private readonly Label _sizeValue = new Label { VerticalAlignment = VerticalAlignment.Center };
    private readonly SignEditorControl _editor = new SignEditorControl { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
    private readonly Label _caret = StatusText.Create("", StatusKind.Muted);
    private SignBlock _shown;
    private bool _syncing;
    private float _fit = 1f;

    public SignWindow() : base("Sign")
    {
        Name = "Sign";
        _colors.AddThemeConstantOverride("separation", 3f);
        _colors.AddChild(_bold);
        _colors.AddChild(_defaultSwatch);
        _defaultSwatch.Clicked += () => SignUI.Document?.ApplyColor(null);
        var swatches = new List<ColorSwatch>();
        foreach (var name in SignUI.QuickColors)
        {
            if (!SignMarkup.TryParseColor(name, out var color)) continue;
            var swatch = _colors.AddChild(new ColorSwatch(color, SwatchSize, name) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var picked = color;
            swatch.Clicked += () => SignUI.Document?.ApplyColor(picked);
            swatches.Add(swatch);
        }
        _swatches = swatches.ToArray();
        _defaultSwatch.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _bold.Pressed += _ => SignUI.Document?.ToggleBold();
        Body.AddChild(_colors);

        var layout = Body.AddChild(new HBoxContainer());
        for (int i = 0; i < SignUI.Alignments.Length; i++)
        {
            var align = SignUI.Alignments[i];
            var button = layout.AddChild(new Button { Text = align.ToString(), ThemeTypeVariation = "SmallButton", ToggleMode = true });
            button.Pressed += _ => SignUI.Document?.SetAlign(align);
            _alignButtons[i] = button;
        }
        var reset = layout.AddChild(new Button { Text = "Reset style", ThemeTypeVariation = "SmallButton", TooltipText = "Removes bold, colours and alignment from the selection, or every style and the text size when nothing is selected" });
        reset.Pressed += _ => SignUI.Document?.ResetStyle();

        var sizeRow = Body.AddChild(new HBoxContainer());
        sizeRow.AddChild(_fill);
        sizeRow.AddChild(new Label { Text = "Size", ThemeTypeVariation = "MutedLabel", VerticalAlignment = VerticalAlignment.Center });
        sizeRow.AddChild(_size);
        sizeRow.AddChild(_sizeValue);
        _fill.Pressed += _ => SignUI.ToggleFill(_fit);
        _size.ValueChanged += value =>
        {
            if (!_syncing) SignUI.SetScale((float)value, _fit);
        };

        Body.AddChild(_editor);
        Body.AddChild(_caret);

        var buttons = Body.AddChild(new HBoxContainer());
        var save = buttons.AddChild(new Button { Text = "Save", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        save.Pressed += _ => SignUI.Save();
        var cancel = buttons.AddChild(new Button { Text = "Cancel", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        cancel.Pressed += _ => SignUI.Close();
    }

    public override bool IsOpen => SignUI.IsOpen;

    protected override void Refresh()
    {
        var sign = SignUI.Sign;
        var document = SignUI.Document;
        var data = sign.Data;
        if (TitleLabel.Text != SignUI.BlockName) TitleLabel.Text = SignUI.BlockName;
        if (!ReferenceEquals(sign, _shown))
        {
            _shown = sign;
            float width = MathF.Max(MinWidth, EditorHeight * data.Width);
            _editor.CustomMinimumSize = new Vector2(MathF.Min(width, EditorHeight * data.Width), EditorHeight);
            _editor.ResetBlink();
            _editor.GrabFocus();
        }

        _fit = SignUI.FitScale();
        var size = SignUI.ClampTextSize(_fit);
        float largest = SignUI.Largest(_fit);

        _editor.Document = document;
        _editor.DefaultColor = data.TextColor;
        _editor.Background = SignUI.BackgroundFor(data.TextColor);
        _editor.SignGlyphWidth = SignUI.SignGlyphWidth;
        _editor.SignLineHeight = SignUI.SignLineHeight;
        _editor.SignPixelsWide = data.PixelsPerBlock * data.Width;
        _editor.SignMargin = data.Margin;
        _editor.TextScale = size.Resolve(_fit);

        _syncing = true;
        _bold.ButtonPressed = document.TypingBold;
        _fill.ButtonPressed = size.Fill;
        for (int i = 0; i < _alignButtons.Length; i++) _alignButtons[i].ButtonPressed = document.CurrentAlign == SignUI.Alignments[i];
        _size.MaxValue = MathF.Max(largest, SignTextSize.MinScale + 0.1f);
        _size.Value = size.Fill ? largest : size.Scale;
        bool locked = size.Fill || largest <= SignTextSize.MinScale;
        _size.MouseFilter = locked ? MouseFilter.Ignore : MouseFilter.Stop;
        _size.Modulate = locked ? new Color4(1f, 1f, 1f, 0.5f) : Color4.White;
        _syncing = false;
        SetText(_sizeValue, $"{_size.Value:0.0}x");
        _size.TooltipText = largest <= SignTextSize.MinScale ? "The text does not fit bigger; shorten the lines or split them" : "Text size for the whole sign";

        _defaultSwatch.Selected = !document.TypingColor.HasValue;
        foreach (var swatch in _swatches) swatch.Selected = document.TypingColor.HasValue && document.TypingColor.Value == swatch.Color;

        var caret = document.Caret;
        StatusText.Set(_caret, $"Line {caret.Line + 1}/{data.MaxLines}   {document.Lines[caret.Line].Chars.Count}/{data.MaxLineLength} characters", StatusKind.Muted);
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
