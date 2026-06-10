using Engine;
using SharpNBT;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;
using System.Collections.Generic;

namespace Spacebox.Game.Generation.Tools
{
    public class AnalyzerSerializer : IBlockSerializer
    {
        public string TagName => "analyzer_data";

        public CompoundTag Serialize(Block block, int flatIndex, Dictionary<string, short> itemsPalette, ref short indexInPalette)
        {
            var analyzer = (AnalyzerBlock)block;

            if (!analyzer.IsScanning && !analyzer.ScanComplete) return null;

            var root = new CompoundTag(TagName);
            root.Add(new IntTag("flat_index", flatIndex));

            if (analyzer.IsScanning)
            {
                root.Add(new ByteTag("is_scanning", 1));

                long currentAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;
                long elapsedTicks = currentAbsoluteTick - analyzer.ScanStartAbsoluteTick;
                if (elapsedTicks < 0) elapsedTicks = 0;

                root.Add(new LongTag("elapsed_ticks", elapsedTicks));
            }
            else if (analyzer.ScanComplete)
            {
                root.Add(new ByteTag("scan_complete", 1));
                root.Add(new LongTag("mass", unchecked((long)analyzer.CachedMass)));

                if (analyzer.CachedMinerals.Count > 0)
                {
                    var mineralsData = new List<long>();
                    foreach (var kvp in analyzer.CachedMinerals)
                    {
                        var blockData = GameAssets.GetBlockDataById(kvp.Key);
                        if (blockData != null)
                        {
                            if (itemsPalette.TryAdd(blockData.Id_string, indexInPalette))
                            {
                                indexInPalette++;
                            }

                            short paletteId = itemsPalette[blockData.Id_string];
                            long packed = ((long)paletteId << 32) | (uint)kvp.Value;
                            mineralsData.Add(packed);
                        }
                    }
                    root.Add(new LongArrayTag("minerals", mineralsData.ToArray()));
                }
            }

            return root;
        }

        public void Deserialize(Block block, CompoundTag tag, string[] paletteItems)
        {
            var analyzer = (AnalyzerBlock)block;

            if (tag.TryGetValue<ByteTag>("is_scanning", out var isScanningTag) && isScanningTag.Value == 1)
            {
                analyzer.IsScanning = true;
                if (tag.TryGetValue<LongTag>("elapsed_ticks", out var tickTag))
                {
                    long currentAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;
                    analyzer.ScanStartAbsoluteTick = currentAbsoluteTick - tickTag.Value;
                }
            }
            else if (tag.TryGetValue<ByteTag>("scan_complete", out var isCompleteTag) && isCompleteTag.Value == 1)
            {
                analyzer.ScanComplete = true;

                if (tag.TryGetValue<LongTag>("mass", out var massTag))
                {
                    analyzer.CachedMass = unchecked((ulong)massTag.Value);
                }

                if (tag.TryGetValue<LongArrayTag>("minerals", out var mineralsTag))
                {
                    analyzer.CachedMinerals.Clear();
                    var arr = mineralsTag.ToArray();
                    foreach (var packed in arr)
                    {
                        short paletteId = (short)(packed >> 32);
                        int count = (int)(packed & 0xFFFFFFFF);

                        if (paletteId >= 0 && paletteId < paletteItems.Length)
                        {
                            string blockStringId = paletteItems[paletteId];
                            var blockData = GameAssets.GetBlockByFullID(blockStringId);
                            if (blockData != null)
                            {
                                analyzer.CachedMinerals[blockData.Id] = count;
                            }
                        }
                    }
                }
            }
        }
    }
}