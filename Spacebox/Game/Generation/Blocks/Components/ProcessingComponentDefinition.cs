namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class ProcessingComponentDefinition : BlockComponentDefinition
{
    public const string ComponentName = "processing";

    public ProcessingComponentDefinition(string recipeType, ushort power, float efficiency)
    {
        RecipeType = recipeType ?? "";
        Power = power;
        Efficiency = efficiency;
    }

    public string RecipeType { get; }
    public ushort Power { get; }
    public float Efficiency { get; }

    public override string Name => ComponentName;
    public override BlockComponentFeatures Features => BlockComponentFeatures.Container | BlockComponentFeatures.NetworkState;
    public override BlockComponent Create() => new ProcessingComponent(this);
}
