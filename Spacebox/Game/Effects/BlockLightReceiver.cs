using Engine;
using Engine.Components;
using Engine.Light;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.Effects;

public sealed class BlockLightReceiver : Component
{
    private const float SampleInterval = 0.1f;
    private const float MaxCameraDistance = 96f;
    private const int MaxDepth = 3;

    private readonly BlockLightProbe _probe = new BlockLightProbe(SampleInterval);

    public Vector3 Value => _probe.Value;

    public override void OnUpdate()
    {
        var camera = Camera.Main;
        if (camera == null || Owner == null) return;
        if (Vector3.DistanceSquared(camera.Position, Owner.Position) > MaxCameraDistance * MaxCameraDistance) return;

        _probe.Update(Owner.Position, Time.Delta);
        Apply(Owner, _probe.Value, 0);
    }

    private static void Apply(Node3D node, Vector3 light, int depth)
    {
        if (node is IExtraLightTarget nodeTarget) nodeTarget.ExtraLight = light;

        var components = node.Components;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i] is IExtraLightTarget target) target.ExtraLight = light;
        }

        if (depth >= MaxDepth) return;

        var children = node.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].HasComponents || children[i] is IExtraLightTarget || children[i].Children.Count > 0)
                Apply(children[i], light, depth + 1);
        }
    }
}
