using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player;

public sealed class BlockLightProbe
{
    private const float Smoothing = 8f;

    public Vector3 Value { get; private set; }

    public void Update(Vector3 worldPosition, float deltaTime)
    {
        var target = BlockLightSampler.Sample(worldPosition);
        Value = Vector3.Lerp(Value, target, 1f - MathF.Exp(-Smoothing * deltaTime));
    }
}
