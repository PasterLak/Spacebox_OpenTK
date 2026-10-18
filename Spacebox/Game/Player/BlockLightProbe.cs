using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public sealed class BlockLightProbe
{
    private const float Smoothing = 8f;

    private readonly float _sampleInterval;
    private float _sampleTimer;
    private Vector3 _target;

    public BlockLightProbe(float sampleInterval = 0f)
    {
        _sampleInterval = sampleInterval;
    }

    public Vector3 Value { get; private set; }

    public void Update(Vector3 worldPosition, float deltaTime)
    {
        _sampleTimer -= deltaTime;
        if (_sampleTimer <= 0f)
        {
            _sampleTimer = _sampleInterval;
            _target = BlockLightSampler.Sample(worldPosition);
        }

        Value = Vector3.Lerp(Value, _target, 1f - MathF.Exp(-Smoothing * deltaTime));
    }
}
