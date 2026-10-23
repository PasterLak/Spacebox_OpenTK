namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class PoweredLightComponentDefinition : BlockComponentDefinition
{
    public const string ComponentName = "light";

    public static readonly PoweredLightComponentDefinition Instance = new PoweredLightComponentDefinition();

    private PoweredLightComponentDefinition()
    {
    }

    public override string Name => ComponentName;
    public override BlockComponent Create() => new PoweredLightComponent();
}
