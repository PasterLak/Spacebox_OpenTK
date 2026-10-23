using Engine;

namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PoweredLightComponent : BlockComponent
{
    public const float SourceLevel = 15f;

    private Color3Byte _color;

    public bool IsLit => Host.IsLight;

    protected override void OnAttached()
    {
        _color = Host.LightColor;
        Apply(false);
    }

    public override void OnActiveStateChanged(bool isActive)
    {
        Apply(isActive);

        var chunk = Host.chunk;
        if (chunk == null || chunk.IsDisposed) return;

        chunk.RequestRelight();
    }

    private void Apply(bool lit)
    {
        bool on = lit && _color != Color3Byte.Black;
        Host.IsLight = on;
        Host.LightLevel = on ? SourceLevel : 0f;
        Host.LightColor = on ? _color : Color3Byte.Zero;
    }
}
