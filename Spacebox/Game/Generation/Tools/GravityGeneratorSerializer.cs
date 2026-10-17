using SharpNBT;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Tools;

public class GravityGeneratorSerializer : IBlockSerializer
{
    public string TagName => "gravity_generator_data";

    public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
    {
        var generator = (GravityGeneratorBlock)block;
        if (generator.IsDefaultConfiguration) return null;

        var root = new CompoundTag(TagName);
        root.Add(new IntTag("flat_index", flatIndex));
        root.Add(new IntTag("radius", generator.RadiusSetting));
        root.Add(new FloatTag("strength", generator.Strength));
        root.Add(new ByteTag("enabled", (byte)(generator.IsGravityEnabled ? 1 : 0)));
        root.Add(new ByteTag("inside_only", (byte)(generator.InsideOnly ? 1 : 0)));
        return root;
    }

    public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
    {
        var generator = (GravityGeneratorBlock)block;
        var data = generator.Data;

        int radius = tag.TryGetValue<IntTag>("radius", out var radiusTag) ? radiusTag.Value : data.Radius;
        float strength = tag.TryGetValue<FloatTag>("strength", out var strengthTag) ? strengthTag.Value : data.Strength;
        bool enabled = !tag.TryGetValue<ByteTag>("enabled", out var enabledTag) || enabledTag.Value == 1;
        bool insideOnly = tag.TryGetValue<ByteTag>("inside_only", out var insideTag) && insideTag.Value == 1;

        generator.Configure(radius, strength, enabled, insideOnly);
    }
}
