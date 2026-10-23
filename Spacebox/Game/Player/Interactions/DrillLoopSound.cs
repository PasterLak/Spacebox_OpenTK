using Engine;
using Engine.Audio;

namespace Spacebox.Game.Player.Interactions;

public sealed class DrillLoopSound : IDisposable
{
    private readonly AudioSource _source;

    public DrillLoopSound(string soundName)
    {
        if (Application.IsHeadless || string.IsNullOrEmpty(soundName)) return;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip) || clip == null) return;

        _source = new AudioSource(clip, AudioChannel.Effects) { IsLooped = true, Volume = 0f };
    }

    public DrillSoundMixer Mixer { get; } = new DrillSoundMixer();

    public void Update(float deltaTime)
    {
        Mixer.Update(deltaTime);
        if (_source == null || _source.IsDisposed) return;

        if (!Mixer.IsAudible)
        {
            if (_source.IsPlaying) _source.Stop();
            return;
        }

        _source.Volume = Mixer.Volume;
        _source.Pitch = Mixer.Pitch;
        if (!_source.IsPlaying) _source.Play();
    }

    public void Stop()
    {
        Mixer.Silence();
        if (_source != null && _source.IsPlaying) _source.Stop();
    }

    public void Dispose()
    {
        _source?.Dispose();
    }
}
