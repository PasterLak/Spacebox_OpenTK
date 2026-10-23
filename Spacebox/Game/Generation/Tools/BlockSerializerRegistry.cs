using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Blocks.Components;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.Generation.Doors;

namespace Spacebox.Game.Generation.Tools
{
    public static class BlockSerializerRegistry
    {
        private static readonly ConcurrentDictionary<Type, IBlockSerializer> Resolved = new ConcurrentDictionary<Type, IBlockSerializer>();

        private static readonly Dictionary<Type, IBlockSerializer> Serializers = new Dictionary<Type, IBlockSerializer>
        {
            { typeof(StorageBlock), new StorageBlockSerializer() },
            { typeof(ComponentBlock), new ComponentBlockSerializer() },
            { typeof(DisassemblerBlock), new DisassemblerSerializer() },
            { typeof(AnalyzerBlock), new AnalyzerSerializer() },
            { typeof(OxygenGeneratorBlock), new OxygenGeneratorSerializer() },
            { typeof(DoorBlock), new DoorSerializer() },
            { typeof(GravityGeneratorBlock), new GravityGeneratorSerializer() },
            { typeof(SignBlock), new SignSerializer() },
            { typeof(PictureFrameBlock), new PictureFrameSerializer() },
            { typeof(DnaReconstructorBlock), new DnaReconstructorSerializer() },
            { typeof(PipeBlock), new PipeSerializer() },
            { typeof(ShipConsoleBlock), new ShipConsoleSerializer() },
            { typeof(ClaimBeaconBlock), new ClaimBeaconSerializer() }
        };

        public static IBlockSerializer GetSerializer(Type blockType)
        {
            if (Resolved.TryGetValue(blockType, out var cached))
            {
                return cached;
            }

            var serializer = Find(blockType);
            Resolved[blockType] = serializer;
            return serializer;
        }

        private static IBlockSerializer Find(Type blockType)
        {
            if (Serializers.TryGetValue(blockType, out var exactSerializer))
            {
                return exactSerializer;
            }

            foreach (var kvp in Serializers)
            {
                if (kvp.Key.IsAssignableFrom(blockType))
                {
                    return kvp.Value;
                }
            }

            return null;
        }
    }
}