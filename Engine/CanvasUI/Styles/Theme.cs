using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class Theme
{
    private static Theme _fallback;

    private readonly Dictionary<(string Type, string Name), StyleBox> _styles = new Dictionary<(string, string), StyleBox>();
    private readonly Dictionary<(string Type, string Name), Color4> _colors = new Dictionary<(string, string), Color4>();
    private readonly Dictionary<(string Type, string Name), float> _constants = new Dictionary<(string, string), float>();
    private readonly Dictionary<string, string> _variationBases = new Dictionary<string, string>();

    public CanvasFont DefaultFont { get; set; }
    public float DefaultFontSize { get; set; } = 12f;

    public static Theme Fallback => _fallback ??= CreateFallback();

    public Theme SetStyleBox(string type, string name, StyleBox style)
    {
        _styles[(type, name)] = style;
        return this;
    }

    public Theme SetColor(string type, string name, Color4 color)
    {
        _colors[(type, name)] = color;
        return this;
    }

    public Theme SetConstant(string type, string name, float value)
    {
        _constants[(type, name)] = value;
        return this;
    }

    public Theme SetTypeVariation(string variation, string baseType)
    {
        _variationBases[variation] = baseType;
        return this;
    }

    public bool TryGetStyleBox(string type, string name, out StyleBox style) => TryGet(_styles, type, name, out style);

    public bool TryGetColor(string type, string name, out Color4 color) => TryGet(_colors, type, name, out color);

    public bool TryGetConstant(string type, string name, out float value) => TryGet(_constants, type, name, out value);

    private bool TryGet<T>(Dictionary<(string, string), T> map, string type, string name, out T value)
    {
        for (int depth = 0; type != null && depth < 8; depth++)
        {
            if (map.TryGetValue((type, name), out value)) return true;
            _variationBases.TryGetValue(type, out type);
        }
        value = default;
        return false;
    }

    private static Theme CreateFallback()
    {
        var border = new Color4(0.35f, 0.39f, 0.47f, 1f);
        var theme = new Theme();
        theme.SetColor("Label", "font_color", new Color4(0.94f, 0.94f, 0.94f, 1f));
        theme.SetColor("Button", "font_color", new Color4(0.94f, 0.94f, 0.94f, 1f));
        theme.SetColor("Button", "font_disabled_color", new Color4(0.5f, 0.5f, 0.55f, 1f));
        theme.SetColor("ProgressBar", "font_color", new Color4(0.94f, 0.94f, 0.94f, 1f));
        theme.SetStyleBox("Panel", "panel", new StyleBoxFlat(new Color4(0.06f, 0.08f, 0.11f, 0.86f)).SetBorder(1f, border));
        theme.SetStyleBox("PanelContainer", "panel", new StyleBoxFlat(new Color4(0.06f, 0.08f, 0.11f, 0.86f), 8f).SetBorder(1f, border));
        theme.SetStyleBox("Button", "normal", new StyleBoxFlat(new Color4(0.18f, 0.2f, 0.24f, 1f), 6f).SetBorder(1f, border));
        theme.SetStyleBox("Button", "hover", new StyleBoxFlat(new Color4(0.23f, 0.25f, 0.31f, 1f), 6f).SetBorder(1f, new Color4(0.67f, 0.69f, 0.73f, 1f)));
        theme.SetStyleBox("Button", "pressed", new StyleBoxFlat(new Color4(0.13f, 0.15f, 0.18f, 1f), 6f).SetBorder(1f, new Color4(1f, 0.77f, 0.16f, 1f)));
        theme.SetStyleBox("Button", "disabled", new StyleBoxFlat(new Color4(0.12f, 0.13f, 0.15f, 1f), 6f).SetBorder(1f, border));
        theme.SetStyleBox("ProgressBar", "background", new StyleBoxFlat(new Color4(0.18f, 0.2f, 0.24f, 1f)));
        theme.SetStyleBox("ProgressBar", "fill", new StyleBoxFlat(new Color4(0.43f, 0.86f, 0.43f, 1f)));
        theme.SetConstant("BoxContainer", "separation", 4f);
        theme.SetConstant("GridContainer", "h_separation", 4f);
        theme.SetConstant("GridContainer", "v_separation", 4f);
        return theme;
    }
}
