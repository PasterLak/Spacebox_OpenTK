namespace Spacebox.Game.GUI.Menu
{
    public class GameWindow : RowsCategoryWindow
    {
        private const int DrawDistanceStep = 50;

        private readonly string[] _languages = { "English" };
        private int _languageIndex = 0;

        public GameWindow(IOptionsNavigator options) : base(options)
        {
        }

        public override string Title => "Game";

        protected override IReadOnlyList<OptionRow> BuildRows()
        {
            return new OptionRow[]
            {
                new SliderRow("Draw Distance", "How far asteroids, ships and their chunks are drawn and generated, in blocks. Lower values give more FPS.", GameplaySettings.MinDrawDistance, GameplaySettings.MaxDrawDistance,
                    () => Settings.Gameplay.DrawDistance,
                    v => Settings.Gameplay.DrawDistance = Math.Clamp(v / DrawDistanceStep * DrawDistanceStep, GameplaySettings.MinDrawDistance, GameplaySettings.MaxDrawDistance),
                    SliderRow.Suffix(" blocks")),
                new ChoiceRow("Language", "Select the game’s display language", _languages, LanguageIndex, index => Settings.Gameplay.Language = _languages[index]),
                new SliderRow("Mouse Sensitivity", "How fast the camera turns when you move the mouse. 100% is the default speed.", 10, 300, () => Settings.Gameplay.MouseSensitivity, v => Apply(g => g.MouseSensitivity = v), SliderRow.Suffix("%")),
                new CheckRow("Invert Mouse Y", "Moving the mouse up looks down, like in flight controls.", () => Settings.Gameplay.InvertMouseY, v => Apply(g => g.InvertMouseY = v)),
                new CheckRow("Camera Sway", "The view sways slightly while you move. Turn it off if it makes you dizzy.", () => Settings.Gameplay.CameraSway, v => Settings.Gameplay.CameraSway = v),
                new SectionRow("Movement"),
                new CheckRow("Auto Sprint", "Press the sprint key once to start sprinting and again to stop, instead of holding it. Sprinting stops when you stop moving and uses power while flying.", () => Settings.Gameplay.AutoSprint, v => Settings.Gameplay.AutoSprint = v),
                new CheckRow("Auto Jump", "In gravity you jump onto one block high steps automatically when you walk into them.", () => Settings.Gameplay.AutoJump, v => Settings.Gameplay.AutoJump = v),
                new SectionRow("Saving"),
                new CheckRow("Autosave", "Saves your own worlds (single player and hosted games) automatically. Without it only the Save button in the pause menu saves.", () => Settings.Gameplay.Autosave, v => Settings.Gameplay.Autosave = v),
                new SliderRow("Autosave Interval", "Minutes between automatic saves.", GameplaySettings.MinAutosaveMinutes, GameplaySettings.MaxAutosaveMinutes, () => Settings.Gameplay.AutosaveMinutes, v => Settings.Gameplay.AutosaveMinutes = v, SliderRow.Suffix(" min"))
            };
        }

        private int LanguageIndex()
        {
            _languageIndex = Settings.Gameplay.Language == "English" ? 0 : 0; // Currently only English is supported
            return _languageIndex;
        }

        private static void Apply(Action<GameplaySettings> change)
        {
            var game = Settings.Gameplay;
            change(game);
            SettingsApplier.ApplyGameplay(game);
        }
    }
}
