using Engine.Audio;

namespace Spacebox.Game.Effects.Machines;

public sealed class MachineLoopVoices
{
    public const int MaxVoices = 4;
    public const float Volume = 0.5f;
    public const float FadeInSeconds = 0.8f;
    public const float FadeOutSeconds = 0.6f;
    public const float MinDistance = 1.5f;
    public const float Rolloff = 1.6f;

    private sealed class Voice
    {
        public AudioSource Source;
        public MachineSoundSource Target;
        public float Volume;
        public bool Wanted;
    }

    private readonly Voice[] _voices = new Voice[MaxVoices];
    private readonly MachineSoundSource[] _nearest = new MachineSoundSource[MaxVoices];
    private readonly float _maxDistance;

    public MachineLoopVoices(float maxDistance)
    {
        _maxDistance = maxDistance;
        for (int i = 0; i < _voices.Length; i++) _voices[i] = new Voice();
    }

    public void Assign(IReadOnlyList<MachineSoundSource> working)
    {
        int count = SelectNearest(working);
        for (int i = 0; i < _voices.Length; i++) _voices[i].Wanted = false;

        for (int i = 0; i < count; i++)
        {
            var source = _nearest[i];
            var voice = FindVoice(source) ?? Claim(source);
            if (voice != null) voice.Wanted = true;
            _nearest[i] = null;
        }
    }

    public void Update(float deltaTime)
    {
        float dt = Math.Max(0f, deltaTime);
        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            if (voice.Target == null) continue;

            bool alive = voice.Target.IsValid;
            float target = voice.Wanted && alive ? Volume : 0f;
            float fade = target > voice.Volume ? FadeInSeconds : FadeOutSeconds;
            float step = Volume / fade * dt;
            voice.Volume = target > voice.Volume ? Math.Min(target, voice.Volume + step) : Math.Max(target, voice.Volume - step);

            if (voice.Volume <= 0f && !voice.Wanted || !alive)
            {
                Release(voice);
                continue;
            }

            voice.Source.Position = voice.Target.WorldPosition;
            voice.Source.Volume = voice.Volume;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            voice.Source?.Dispose();
            voice.Source = null;
            voice.Target = null;
            voice.Volume = 0f;
            voice.Wanted = false;
        }
        Array.Clear(_nearest);
    }

    private int SelectNearest(IReadOnlyList<MachineSoundSource> working)
    {
        int count = 0;
        for (int i = 0; i < working.Count; i++)
        {
            var source = working[i];
            int slot;
            if (count < _nearest.Length) slot = count++;
            else if (source.DistanceSquared < _nearest[count - 1].DistanceSquared) slot = count - 1;
            else continue;

            _nearest[slot] = source;
            while (slot > 0 && _nearest[slot - 1].DistanceSquared > _nearest[slot].DistanceSquared)
            {
                (_nearest[slot - 1], _nearest[slot]) = (_nearest[slot], _nearest[slot - 1]);
                slot--;
            }
        }
        return count;
    }

    private Voice FindVoice(MachineSoundSource source)
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            if (ReferenceEquals(_voices[i].Target, source)) return _voices[i];
        }
        return null;
    }

    private Voice Claim(MachineSoundSource source)
    {
        if (!GameAssets.Sounds.TryGetValue(source.WorkingSound, out var clip) || clip == null) return null;

        for (int i = 0; i < _voices.Length; i++)
        {
            var voice = _voices[i];
            if (voice.Target != null) continue;

            if (voice.Source == null || voice.Source.IsDisposed)
            {
                voice.Source = new AudioSource(clip, AudioChannel.Effects);
                voice.Source.Setup3D(MinDistance, _maxDistance, Rolloff);
                voice.Source.IsLooped = true;
            }
            else if (!ReferenceEquals(voice.Source.Clip, clip))
            {
                voice.Source.Clip = clip;
            }

            voice.Target = source;
            voice.Volume = 0f;
            voice.Source.Volume = 0f;
            voice.Source.Position = source.WorldPosition;
            voice.Source.Play();
            return voice;
        }
        return null;
    }

    private static void Release(Voice voice)
    {
        if (voice.Source != null && voice.Source.IsPlaying) voice.Source.Stop();
        voice.Target = null;
        voice.Volume = 0f;
        voice.Wanted = false;
    }
}
