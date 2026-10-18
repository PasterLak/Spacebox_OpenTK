using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Mobs;

public sealed class MobAnimator
{
    private const float BiteDuration = 0.35f;
    private const float HurtDuration = 0.25f;
    private const float ChewSpeed = 14f;
    private const float ChewAmount = 0.07f;
    private const float BreathSpeed = 3f;
    private const float BreathAmount = 0.03f;
    private const float BiteStretch = 0.3f;
    private const float HurtSquash = 0.18f;

    private readonly Node3D _node;
    private readonly float _baseScale;
    private float _time;
    private float _biteTimer;
    private float _hurtTimer;

    public MobAnimator(Node3D node, float baseScale, float phase)
    {
        _node = node;
        _baseScale = baseScale;
        _time = phase;
    }

    public void PlayBite()
    {
        _biteTimer = BiteDuration;
    }

    public void PlayHurt()
    {
        _hurtTimer = HurtDuration;
    }

    public void Update(float deltaTime, MobAction action)
    {
        _time += deltaTime;

        float breath = MathF.Sin(_time * BreathSpeed) * BreathAmount;
        var scale = new Vector3(1f + breath, 1f + breath, 1f - breath);

        if (action == MobAction.Eating)
        {
            float chew = MathF.Sin(_time * ChewSpeed) * ChewAmount;
            scale += new Vector3(-chew * 0.5f, -chew * 0.5f, chew);
        }

        if (_biteTimer > 0f)
        {
            _biteTimer -= deltaTime;
            float k = MathF.Sin((1f - MathF.Max(0f, _biteTimer) / BiteDuration) * MathF.PI);
            scale += new Vector3(-BiteStretch * 0.3f * k, -BiteStretch * 0.3f * k, BiteStretch * k);
        }

        if (_hurtTimer > 0f)
        {
            _hurtTimer -= deltaTime;
            scale *= 1f - HurtSquash * MathF.Max(0f, _hurtTimer) / HurtDuration;
        }

        _node.Scale = scale * _baseScale;
    }
}
