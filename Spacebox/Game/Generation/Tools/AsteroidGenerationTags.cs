using SharpNBT;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Generation.Tools;

public static class AsteroidGenerationTags
{
    public static void Write(CompoundTag root, NotGeneratedEntity data)
    {
        if (data == null || !GeneratorVersions.UsesRules(data.generatorVersion)) return;

        root.Add(new IntTag(NBTKey.ENTITY.generator_version, data.generatorVersion));
        root.Add(new FloatTag(NBTKey.ENTITY.generation_distance, data.generationDistance));
        if (data.biome != null) root.Add(new StringTag(NBTKey.ENTITY.generation_biome, data.biome.IdString));
    }

    public static void Read(CompoundTag tag, NotGeneratedEntity data)
    {
        if (data == null) return;

        if (!tag.TryGetValue<IntTag>(NBTKey.ENTITY.generator_version, out var versionTag))
        {
            data.generatorVersion = GeneratorVersions.Legacy;
            return;
        }

        data.generatorVersion = GeneratorVersions.Normalize(versionTag.Value);
        if (tag.TryGetValue<FloatTag>(NBTKey.ENTITY.generation_distance, out var distanceTag)) data.generationDistance = distanceTag.Value;
        data.biome = tag.TryGetValue<StringTag>(NBTKey.ENTITY.generation_biome, out var biomeTag) ? World.WorldGenerator?.GetBiome(biomeTag.Value) : null;
    }
}
