namespace Spacebox.Game.GUI.Menu;

public class InterfaceWindow : SettingsCategoryWindow
{
    private const uint FontTestLength = 256;

    private readonly IOptionsNavigator _options;
    private readonly string[] _fontNames = InterfaceFonts.All.Select(font => font.Name).ToArray();
    private string _fontTest = "Hello Äöüß Привет éèçà ñ¡¿";

    public InterfaceWindow(IOptionsNavigator options)
    {
        _options = options;
    }

    public override void Render()
    {
        var settings = Settings.Interface;

        SettingsUI.Render("Interface", "Interface", 8,
            (listSize, rowH) =>
            {
                var rows = OptionsRows.Begin("table##interface", listSize.X);

                rows.Section("Font");
                int font = InterfaceFonts.IndexOf(settings.Font);
                if (rows.Combo("Font", "Font of the whole interface. Fonts and their licenses are listed in Resources/Fonts/FONTS.md.", ref font, _fontNames))
                {
                    var chosen = InterfaceFonts.All[font];
                    Apply(settings, () =>
                    {
                        settings.Font = chosen.Id;
                        settings.FontSize = chosen.NativeSize;
                    });
                }

                int fontSize = settings.FontSize;
                if (rows.Slider("Font Size", "Size of the interface font in pixels before window scaling. Pixel fonts look sharpest at their native size; choosing a font resets it.", ref fontSize, InterfaceFonts.MinSize, InterfaceFonts.MaxSize, "%d px"))
                    Apply(settings, () => settings.FontSize = fontSize);

                rows.TextInput("Font Test", "Type any text to see how the selected font draws it. Not saved.", ref _fontTest, FontTestLength);

                rows.Section("Layout");
                int design = Math.Max(0, Array.IndexOf(Canvas.GameUiDesigns.Ids, settings.Design));
                if (rows.Combo("Design", "Look of the game interface: Classic keeps the familiar grey blocks, Modern uses dark translucent panels like the tutorial.", ref design, Canvas.GameUiDesigns.Names))
                    Apply(settings, () => settings.Design = Canvas.GameUiDesigns.Ids[design]);

                int scale = (int)MathF.Round(settings.UiScale * 100f);
                if (rows.Slider("UI Scale", "Size of the game interface (hotbar, bars and new windows) on top of the automatic size for your screen.", ref scale, (int)(InterfaceSettings.MinUiScale * 100), (int)(InterfaceSettings.MaxUiScale * 100), "%d %%"))
                    Apply(settings, () => settings.UiScale = scale / 100f);

                rows.End();
            },
            _options.Save,
            _options.Back
        );
    }

    private static void Apply(InterfaceSettings settings, Action change)
    {
        change();
        SettingsApplier.ApplyInterface(settings);
    }
}
