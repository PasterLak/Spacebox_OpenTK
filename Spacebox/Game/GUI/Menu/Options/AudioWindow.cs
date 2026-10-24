namespace Spacebox.Game.GUI.Menu
{
    public class AudioWindow : RowsCategoryWindow
    {
        public AudioWindow(IOptionsNavigator options) : base(options)
        {
        }

        public override string Title => "Audio";

        protected override IReadOnlyList<OptionRow> BuildRows()
        {
            var percent = SliderRow.Suffix("%");
            var outputs = AudioOutputList.Build(Engine.Audio.AudioDevice.OutputDevices(), Settings.Audio.OutputDevice);
            return new OptionRow[]
            {
                new ChoiceRow("Output Device", "Where the game plays its sound. System default follows the device chosen in the operating system; if the chosen device is unplugged, the system default is used until it comes back.", outputs.Labels,
                    () => outputs.IndexOf(Settings.Audio.OutputDevice), index => Apply(a => a.OutputDevice = outputs.DeviceAt(index))),
                new SliderRow("Master Volume", "Volume of everything the game plays. The other sliders are scaled by this one.", 0, 100, () => Settings.Audio.Master, v => Apply(a => a.Master = v), percent),
                new SliderRow("Music Volume", "Volume of the menu music.", 0, 100, () => Settings.Audio.Music, v => Apply(a => a.Music = v), percent),
                new SliderRow("Ambient Volume", "Volume of the space background and mood sounds.", 0, 100, () => Settings.Audio.Ambient, v => Apply(a => a.Ambient = v), percent),
                new SliderRow("Effects Volume", "Volume of gameplay sounds: blocks, tools, weapons, explosions, machines, mobs and players.", 0, 100, () => Settings.Audio.Effects, v => Apply(a => a.Effects = v), percent),
                new SliderRow("Interface Volume", "Volume of buttons, inventory, storages and other windows.", 0, 100, () => Settings.Audio.UI, v => Apply(a => a.UI = v), percent),
                new CheckRow("Mute When Unfocused", "Silences the game while its window is not active.", () => Settings.Audio.MuteWhenUnfocused, v => Apply(a => a.MuteWhenUnfocused = v)),
                new CheckRow("Menu Music", "Plays music in the main menu. Same as the note button in the menu corner.", () => Settings.Audio.MenuMusic, v => Apply(a => a.MenuMusic = v))
            };
        }

        private static void Apply(Action<AudioSettings> change)
        {
            var audio = Settings.Audio;
            change(audio);
            SettingsApplier.ApplyAudio(audio);
        }
    }
}
