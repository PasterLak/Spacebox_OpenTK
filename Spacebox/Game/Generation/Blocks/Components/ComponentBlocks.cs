using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks.Components;

public static class ComponentBlocks
{
    public static ComponentBlock Create(BlockData data) => Create(data, data.Components ??= BlockComponentRegistry.Resolve(data, null));

    public static ComponentBlock Create(BlockData data, BlockComponentDefinition[] definitions)
    {
        var features = FeaturesOf(definitions);
        bool container = (features & BlockComponentFeatures.Container) != 0;
        bool state = (features & BlockComponentFeatures.NetworkState) != 0;

        if (container && state) return new StatefulContainerComponentBlock(data, definitions);
        if (container) return new ContainerComponentBlock(data, definitions);
        if (state) return new StatefulComponentBlock(data, definitions);
        return new ComponentBlock(data, definitions);
    }

    public static BlockComponentFeatures FeaturesOf(BlockComponentDefinition[] definitions)
    {
        var features = BlockComponentFeatures.None;
        if (definitions == null) return features;

        for (int i = 0; i < definitions.Length; i++)
            features |= definitions[i].Features;
        return features;
    }
}
