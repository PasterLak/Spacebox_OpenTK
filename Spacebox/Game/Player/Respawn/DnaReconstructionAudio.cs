using Engine;
using Engine.Audio;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Player.Respawn;

public sealed class DnaReconstructionAudio : IReconstructionEffects
{
    private const float FadeInEnd = 0.06f;
    private const float FadeOutStart = 0.9f;
    private const float StartPitch = 0.92f;
    private const float EndPitch = 1.12f;
    private const int FirstStageStep = 1;
    private const int LastStageStep = 3;

    private readonly DnaReconstructorSoundsJSON _sounds;
    private AudioSource _loop;
    private AudioSource _stage;
    private int _step = -1;

    public DnaReconstructionAudio(DnaReconstructorSoundsJSON sounds)
    {
        _sounds = sounds ?? new DnaReconstructorSoundsJSON();
    }

    public void Begin()
    {
        End();
        _loop = CreateSource(_sounds.Loop);
        if (_loop == null) return;

        _loop.IsLooped = true;
        _loop.Volume = 0f;
        _loop.Pitch = StartPitch;
        _loop.Play();
    }

    public void Update(float progress, int step)
    {
        if (_loop != null)
        {
            float fade = Math.Min(Math.Clamp(progress / FadeInEnd, 0f, 1f), Math.Clamp((1f - progress) / (1f - FadeOutStart), 0f, 1f));
            _loop.Volume = fade * _sounds.Volume;
            _loop.Pitch = StartPitch + (EndPitch - StartPitch) * progress;
        }

        if (step == _step) return;
        _step = step;
        if (step >= FirstStageStep && step <= LastStageStep) PlayStage();
    }

    public void End()
    {
        _loop?.Dispose();
        _loop = null;
        _stage?.Dispose();
        _stage = null;
        _step = -1;
    }

    private void PlayStage()
    {
        _stage ??= CreateSource(_sounds.Stage);
        if (_stage == null) return;

        if (_stage.IsPlaying) _stage.Stop();
        _stage.Volume = _sounds.Volume;
        _stage.Play();
    }

    private static AudioSource CreateSource(string soundName)
    {
        if (Application.IsHeadless || string.IsNullOrEmpty(soundName)) return null;
        if (!GameAssets.Sounds.TryGetValue(soundName, out var clip)) return null;
        return new AudioSource(clip, AudioChannel.Effects);
    }
}
