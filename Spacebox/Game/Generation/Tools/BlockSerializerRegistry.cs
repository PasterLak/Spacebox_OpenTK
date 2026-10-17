using System;
using System.Collections.Generic;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Doors;

namespace Spacebox.Game.Generation.Tools
{
    public static class BlockSerializerRegistry
    {
        private static readonly Dictionary<Type, IBlockSerializer> Serializers = new Dictionary<Type, IBlockSerializer>
        {
            { typeof(StorageBlock), new StorageBlockSerializer() },
            { typeof(ResourceProcessingBlock), new ResourceProcessingSerializer() },
            { typeof(AnalyzerBlock), new AnalyzerSerializer() },
            { typeof(OxygenGeneratorBlock), new OxygenGeneratorSerializer() },
            { typeof(DoorBlock), new DoorSerializer() },
            { typeof(GravityGeneratorBlock), new GravityGeneratorSerializer() }
        };

        public static IBlockSerializer GetSerializer(Type blockType)
        {
            if (Serializers.TryGetValue(blockType, out var exactSerializer))
            {
                return exactSerializer;
            }

            foreach (var kvp in Serializers)
            {
                if (kvp.Key.IsAssignableFrom(blockType))
                {
                    Serializers[blockType] = kvp.Value;
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}