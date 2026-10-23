namespace Spacebox.Game.GUI.Menu;

public class InterfaceWindow : RowsCategoryWindow
{
    private const uint FontTestLength = 256;

    private readonly string[] _fontNames = InterfaceFonts.All.Select(font => font.Name).ToArray();
    private string _fontTest = "Hello Äöüß Привет éèçà ñ¡¿";

    public InterfaceWindow(IOptionsNavigator options) : base(options)
    {
    }

    public override string Title => "Interface";

    protected override IReadOnlyList<OptionRow> BuildRows()
    {
        return new OptionRow[]
        {
            new SectionRow("Font"),
            new ChoiceRow("Font", "Font of the whole interface. Fonts and their licenses are listed in Resources/Fonts/FONTS.md.", _fontNames, () => InterfaceFonts.IndexOf(Settings.Interface.Font), font =>
            {
                var chosen = InterfaceFonts.All[font];
                Apply(settings =>
                {
                    settings.Font = chosen.Id;
                    settings.FontSize = chosen.NativeSize;
                });
            }),
            new SliderRow("Font Size", "Size of the interface font in pixels before window scaling. Pixel fonts look sharpest at their native size; choosing a font resets it.", InterfaceFonts.MinSize, InterfaceFonts.MaxSize, () => Settings.Interface.FontSize, v => Apply(settings => settings.FontSize = v), SliderRow.Suffix(" px")),
            new TextRow("Font Test", "Type any text to see how the selected font draws it. Not saved.", (int)FontTestLength, () => _fontTest, v => _fontTest = v),
            new SectionRow("Layout"),
            new ChoiceRow("Design", "Look of the game interface: Classic keeps the familiar grey blocks, Modern uses dark translucent panels like the tutorial.", Canvas.GameUiDesigns.Names,
                () => Math.Max(0, Array.IndexOf(Canvas.GameUiDesigns.Ids, Settings.Interface.Design)),
                design => Apply(settings => settings.Design = Canvas.GameUiDesigns.Ids[design])),
            new SliderRow("UI Scale", "Size of the game interface (hotbar, bars and new windows) on top of the automatic size for your screen.", (int)(InterfaceSettings.MinUiScale * 100), (int)(InterfaceSettings.MaxUiScale * 100),
                () => (int)MathF.Round(Settings.Interface.UiScale * 100f), v => Apply(settings => settings.UiScale = v / 100f), SliderRow.Suffix(" %")) { ApplyOnRelease = true }
        };
    }

    private static void Apply(Action<InterfaceSettings> change)
    {
        var settings = Settings.Interface;
        change(settings);
        SettingsApplier.ApplyInterface(settings);
    }
}
