using ImGuiNET;
using System.Numerics;

namespace Spacebox.Game.GUI.Menu
{
    public class GameWindow : SettingsCategoryWindow
    {
        private const int DrawDistanceStep = 50;

        private readonly IOptionsNavigator _options;
        private readonly string[] _languages = { "English" };
        private int _languageIndex = 0;

        public GameWindow(IOptionsNavigator options)
        {
            _options = options;
        }

        public override void Render()
        {
            var game = Settings.Gameplay;
            _languageIndex = Settings.Gameplay.Language == "English" ? 0 : 0; // Currently only English is supported

            SettingsUI.Render("Game", "Game", 5,
                (listSize, rowH) =>
                {
                    var rows = OptionsRows.Begin("table##game", listSize.X);

                    int drawDistance = game.DrawDistance;
                    if (rows.Slider("Draw Distance", "How far asteroids, ships and their chunks are drawn and generated, in blocks. Lower values give more FPS.", ref drawDistance, GameplaySettings.MinDrawDistance, GameplaySettings.MaxDrawDistance, "%d blocks"))
                        game.DrawDistance = Math.Clamp(drawDistance / DrawDistanceStep * DrawDistanceStep, GameplaySettings.MinDrawDistance, GameplaySettings.MaxDrawDistance);

                    rows.Combo("Language", "Select the game’s display language", ref _languageIndex, _languages);

                    int sensitivity = game.MouseSensitivity;
                    if (rows.Slider("Mouse Sensitivity", "How fast the camera turns when you move the mouse. 100% is the default speed.", ref sensitivity, 10, 300, "%d%%"))
                    {
                        game.MouseSensitivity = sensitivity;
                        SettingsApplier.ApplyGameplay(game);
                    }

                    bool invert = game.InvertMouseY;
                    if (rows.Checkbox("Invert Mouse Y", "Moving the mouse up looks down, like in flight controls.", ref invert))
                    {
                        game.InvertMouseY = invert;
                        SettingsApplier.ApplyGameplay(game);
                    }

                    bool sway = game.CameraSway;
                    if (rows.Checkbox("Camera Sway", "The view sways slightly while you move. Turn it off if it makes you dizzy.", ref sway))
                        game.CameraSway = sway;

                    rows.Section("Saving");
                    bool autosave = game.Autosave;
                    if (rows.Checkbox("Autosave", "Saves your own worlds (single player and hosted games) automatically. Without it only the Save button in the pause menu saves.", ref autosave))
                        game.Autosave = autosave;

                    int autosaveMinutes = game.AutosaveMinutes;
                    if (rows.Slider("Autosave Interval", "Minutes between automatic saves.", ref autosaveMinutes, GameplaySettings.MinAutosaveMinutes, GameplaySettings.MaxAutosaveMinutes, "%d min"))
                        game.AutosaveMinutes = autosaveMinutes;

                    rows.End();
                },
                _options.Save,
                _options.Back
            );

            Settings.Gameplay.Language = _languages[_languageIndex];
        }
    }
}
