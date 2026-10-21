using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Network;

namespace Spacebox.Game.Mobs;

public sealed class MobDeathEffects
{
    private const float VisibleDistance = 64f;
    private const string TexturePath = "Resources/Textures/Effects/dust.png";

    private readonly List<MobDeathEffect> _effects = new List<MobDeathEffect>();

    public void Play(MobDeathParticlesJSON settings, Vector3 position)
    {
        if (settings == null || settings.Count <= 0 || !CanPlay(position)) return;

        var texture = Resources.Load<Texture2D>(TexturePath);
        if (texture == null) return;

        _effects.Add(new MobDeathEffect(texture, settings, position, Vector3.One + BlockLightSampler.Sample(position)));
    }

    public void Update(float deltaTime)
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            var effect = _effects[i];
            effect.Update(deltaTime);
            if (!effect.IsFinished) continue;

            effect.Dispose();
            _effects.RemoveAt(i);
        }
    }

    public void Render()
    {
        for (int i = 0; i < _effects.Count; i++) _effects[i].Render();
    }

    public void Clear()
    {
        for (int i = 0; i < _effects.Count; i++) _effects[i].Dispose();
        _effects.Clear();
    }

    private static bool CanPlay(Vector3 position)
    {
        if (GameSession.IsDedicated || !Settings.Graphics.EffectsEnabled || Camera.Main == null) return false;
        return Vector3.DistanceSquared(Camera.Main.Position, position) <= VisibleDistance * VisibleDistance;
    }
}
