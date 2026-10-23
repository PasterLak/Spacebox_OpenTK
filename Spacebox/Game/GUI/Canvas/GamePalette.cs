using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas;

public sealed class GamePalette
{
    public Color4 Panel { get; init; }
    public Color4 PanelLight { get; init; }
    public Color4 PanelDark { get; init; }
    public Color4 TitleBar { get; init; }
    public Color4 TitleText { get; init; }
    public Color4 Text { get; init; }
    public Color4 TextMuted { get; init; }
    public Color4 Field { get; init; }
    public Color4 FieldText { get; init; }
    public Color4 Button { get; init; }
    public Color4 ButtonHover { get; init; }
    public Color4 ButtonPressed { get; init; }
    public Color4 ButtonText { get; init; }
    public Color4 ButtonDisabledText { get; init; }
    public Color4 List { get; init; }
    public Color4 Selected { get; init; }
    public Color4 SelectedText { get; init; }
    public Color4 Hover { get; init; }
    public Color4 Accent { get; init; }
    public Color4 Heading { get; init; }
    public Color4 Error { get; init; }
    public Color4 Warning { get; init; }
    public Color4 Success { get; init; }
    public bool Bevel { get; init; }

    public static readonly GamePalette Classic = new GamePalette
    {
        Panel = new Color4(0.75f, 0.75f, 0.75f, 1f),
        PanelLight = new Color4(0.9f, 0.9f, 0.9f, 1f),
        PanelDark = new Color4(0.5f, 0.5f, 0.5f, 1f),
        TitleBar = new Color4(0.45f, 0.45f, 0.45f, 1f),
        TitleText = Color4.White,
        Text = new Color4(0.13f, 0.13f, 0.13f, 1f),
        TextMuted = new Color4(0.3f, 0.3f, 0.3f, 1f),
        Field = new Color4(0.9f, 0.9f, 0.9f, 1f),
        FieldText = new Color4(0.1f, 0.1f, 0.1f, 1f),
        Button = new Color4(0.66f, 0.66f, 0.66f, 1f),
        ButtonHover = new Color4(0.74f, 0.74f, 0.7f, 1f),
        ButtonPressed = new Color4(0.55f, 0.55f, 0.55f, 1f),
        ButtonText = new Color4(0.1f, 0.1f, 0.1f, 1f),
        ButtonDisabledText = new Color4(0.42f, 0.42f, 0.42f, 1f),
        List = new Color4(0.85f, 0.85f, 0.85f, 1f),
        Selected = new Color4(1f, 0.72f, 0.1f, 1f),
        SelectedText = new Color4(0.08f, 0.08f, 0.08f, 1f),
        Hover = new Color4(0f, 0f, 0f, 0.08f),
        Accent = new Color4(1f, 0.72f, 0.1f, 1f),
        Heading = new Color4(0.45f, 0.27f, 0f, 1f),
        Error = new Color4(0.7f, 0.05f, 0.05f, 1f),
        Warning = new Color4(0.55f, 0.32f, 0f, 1f),
        Success = new Color4(0.05f, 0.45f, 0.1f, 1f),
        Bevel = true
    };

    public static readonly GamePalette Modern = new GamePalette
    {
        Panel = new Color4(0.06f, 0.07f, 0.1f, 0.94f),
        PanelLight = new Color4(0.35f, 0.38f, 0.46f, 1f),
        PanelDark = new Color4(0.35f, 0.38f, 0.46f, 1f),
        TitleBar = new Color4(0.1f, 0.11f, 0.15f, 1f),
        TitleText = new Color4(1f, 0.77f, 0.16f, 1f),
        Text = new Color4(0.93f, 0.93f, 0.95f, 1f),
        TextMuted = new Color4(0.64f, 0.66f, 0.72f, 1f),
        Field = new Color4(0.12f, 0.13f, 0.17f, 1f),
        FieldText = new Color4(0.95f, 0.95f, 0.97f, 1f),
        Button = new Color4(0.16f, 0.17f, 0.22f, 1f),
        ButtonHover = new Color4(0.23f, 0.24f, 0.3f, 1f),
        ButtonPressed = new Color4(0.45f, 0.33f, 0.06f, 1f),
        ButtonText = new Color4(0.95f, 0.95f, 0.97f, 1f),
        ButtonDisabledText = new Color4(0.45f, 0.47f, 0.52f, 1f),
        List = new Color4(0.09f, 0.1f, 0.13f, 1f),
        Selected = new Color4(0.62f, 0.45f, 0.08f, 1f),
        SelectedText = Color4.White,
        Hover = new Color4(1f, 1f, 1f, 0.07f),
        Accent = new Color4(1f, 0.77f, 0.16f, 1f),
        Heading = new Color4(1f, 0.77f, 0.16f, 1f),
        Error = new Color4(1f, 0.45f, 0.38f, 1f),
        Warning = new Color4(1f, 0.75f, 0.3f, 1f),
        Success = new Color4(0.45f, 0.85f, 0.45f, 1f),
        Bevel = false
    };

    public static GamePalette For(GameUiDesign design) => design == GameUiDesign.Modern ? Modern : Classic;
}
