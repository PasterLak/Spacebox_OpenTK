using Engine.Audio;

namespace Spacebox.Game.GUI.Menu;

public class MenuMusicController
{
    private readonly AudioSource _music;
    private bool _isPlaying;

    public MenuMusicController(AudioSource music)
    {
        _music = music;
        _music.IsLooped = true;
        Apply(Settings.Audio.MenuMusic);
    }

    public bool IsEnabled => Settings.Audio.MenuMusic;

    public void Toggle()
    {
        Settings.Audio.MenuMusic = !Settings.Audio.MenuMusic;
        SettingsService.Save(Settings.AsGameSettings());
        Sync();
    }

    public void Sync()
    {
        if (_isPlaying != Settings.Audio.MenuMusic)
            Apply(Settings.Audio.MenuMusic);
    }

    private void Apply(bool enabled)
    {
        _isPlaying = enabled;

        if (enabled)
            _music.Play();
        else
            _music.Stop();
    }
}
