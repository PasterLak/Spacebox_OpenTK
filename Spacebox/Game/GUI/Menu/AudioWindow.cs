using ImGuiNET;
using System.Numerics;

namespace Spacebox.Game.GUI.Menu
{
    public class AudioWindow : MenuWindow
    {
        private readonly IOptionsNavigator _options;

        public AudioWindow(IOptionsNavigator options)
        {
            _options = options;
        }

        public override void Render()
        {
            var audio = Settings.Audio;

            SettingsUI.Render("Audio", "Audio", 5,
                (listSize, rowH) =>
                {
                    var rows = OptionsRows.Begin("table##audio", listSize.X);
                    bool changed = false;

                    int master = audio.Master;
                    changed |= rows.Slider("Master Volume", "Volume of everything the game plays. The other sliders are scaled by this one.", ref master, 0, 100, "%d%%");
                    int music = audio.Music;
                    changed |= rows.Slider("Music Volume", "Volume of the menu music.", ref music, 0, 100, "%d%%");
                    int ambient = audio.Ambient;
                    changed |= rows.Slider("Ambient Volume", "Volume of the space background and mood sounds.", ref ambient, 0, 100, "%d%%");
                    int effects = audio.Effects;
                    changed |= rows.Slider("Effects Volume", "Volume of gameplay sounds: blocks, tools, weapons, explosions, machines, mobs and players.", ref effects, 0, 100, "%d%%");
                    int ui = audio.UI;
                    changed |= rows.Slider("Interface Volume", "Volume of buttons, inventory, storages and other windows.", ref ui, 0, 100, "%d%%");

                    bool mute = audio.MuteWhenUnfocused;
                    changed |= rows.Checkbox("Mute When Unfocused", "Silences the game while its window is not active.", ref mute);
                    bool menuMusic = audio.MenuMusic;
                    changed |= rows.Checkbox("Menu Music", "Plays music in the main menu. Same as the note button in the menu corner.", ref menuMusic);

                    rows.End();

                    if (!changed) return;
                    audio.Master = master;
                    audio.Music = music;
                    audio.Ambient = ambient;
                    audio.Effects = effects;
                    audio.UI = ui;
                    audio.MuteWhenUnfocused = mute;
                    audio.MenuMusic = menuMusic;
                    SettingsApplier.ApplyAudio(audio);
                },
                () =>
                {
                    _options.PlayClick();
                    SettingsService.Save(Settings.AsGameSettings());
                    _options.ShowOptions();
                },
                () =>
                {
                    _options.PlayClick();
                    OptionsMenu.DiscardUnsavedChanges();
                    _options.ShowOptions();
                }
            );
        }
    }
}
