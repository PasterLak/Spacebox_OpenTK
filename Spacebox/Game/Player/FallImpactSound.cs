using Engine;
using Engine.Audio;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player;

public sealed class FallImpactSound : IDisposable
{
    private const string ClipPath = "Resources/Audio/wallHit2.ogg";
    private const float MinVolume = 0.5f;
    private const float VolumePerDamage = 0.02f;
    private const float MinPitch = 0.75f;
    private const float MaxPitch = 0.9f;
    private const float MinDistance = 3f;
    private const float MaxDistance = 60f;
    private const float Rolloff = 1.5f;

    private readonly Random _random = new Random();
    private AudioSource? _source;
    private readonly bool _isSpatial;

    public FallImpactSound(bool isSpatial)
    {
        _isSpatial = isSpatial;
    }

    public void Play(int damage, Vector3 position)
    {
        var source = GetSource();
        if (source.IsPlaying) source.Stop();

        if (_isSpatial) source.Position = position;
        source.Volume = MathHelper.Clamp(MinVolume + damage * VolumePerDamage, 0f, 1f);
        source.Pitch = MinPitch + (float)_random.NextDouble() * (MaxPitch - MinPitch);
        source.Play();
    }

    public void Dispose()
    {
        _source?.Dispose();
        _source = null;
    }

    private AudioSource GetSource()
    {
        if (_source != null) return _source;

        _source = new AudioSource(GameAssets.LoadResource<AudioClip>(ClipPath));
        if (_isSpatial) _source.Setup3D(MinDistance, MaxDistance, Rolloff);
        return _source;
    }
}
