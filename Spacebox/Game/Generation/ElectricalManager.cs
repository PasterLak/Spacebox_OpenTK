using Engine;

namespace Spacebox.Game.Generation
{
    public class ElectricNetworkManager
    {
        public Dictionary<(int x, int y, int z), ElectricalBlock> Blocks = new Dictionary<(int x, int y, int z), ElectricalBlock>();
        Dictionary<int, List<(int x, int y, int z)>> networks = new Dictionary<int, List<(int, int, int)>>();
        Dictionary<(int x, int y, int z), int> blockToNetwork = new Dictionary<(int x, int y, int z), int>();
        Dictionary<int, HashSet<Chunk>> networkChunks = new Dictionary<int, HashSet<Chunk>>();
        Dictionary<(int x, int y, int z), Chunk> blockToChunk = new Dictionary<(int x, int y, int z), Chunk>();

        int nextNetworkId = 1;

        private bool GetOldNetworkActiveState((int x, int y, int z) pos, out int neignborns)
        {

            HashSet<int> adjacentNetworkIds = new HashSet<int>();
            var neighbors = GetNeighbors(pos);
            foreach (var neighbor in neighbors)
            {

                if (Blocks.ContainsKey(neighbor) && blockToNetwork.TryGetValue(neighbor, out int netId))
                {
                    adjacentNetworkIds.Add(netId);
                }
            }
            neignborns = 0;
            if (adjacentNetworkIds.Count == 0)
                return false;

            int totalGen = 0;
            int totalCons = 0;

            foreach (var netId in adjacentNetworkIds)
            {
                if (networks.TryGetValue(netId, out List<(int x, int y, int z)> coords))
                {
                    foreach (var coord in coords)
                    {
                        if (Blocks.TryGetValue(coord, out ElectricalBlock b))
                        {
                            if ((b.EFlags & ElectricalFlags.CanGenerate) != 0)
                                totalGen += b.GenerationRate;
                            if ((b.EFlags & ElectricalFlags.CanConsume) != 0)
                                totalCons += b.ConsumptionRate;
                        }
                    }
                    neignborns++;
                }
            }

            return totalGen >= totalCons;
        }

        private bool IsBlockActive((int x, int y, int z) pos)
        {
            if (Blocks.TryGetValue(pos, out ElectricalBlock b))
            {
                return b.IsActive;
            }

            return false;
        }
        public void AddBlockFast((int x, int y, int z) pos, ElectricalBlock block, Chunk parentChunk)
        {
            Blocks[pos] = block;
            blockToChunk[pos] = parentChunk;
           

        }
        public void AddBlock((int x, int y, int z) pos, ElectricalBlock block, Chunk parentChunk)
        {
            if (block.EFlags == ElectricalFlags.None) return;

            bool lastState = GetOldNetworkActiveState(pos, out var neignborns);
            Blocks[pos] = block;
            blockToChunk[pos] = parentChunk;
            RebuildAllNetworks();

            UpdateAllNetworks(lastState, neignborns > 1);
        }

        public void RemoveBlock((int x, int y, int z) pos)
        {
            bool lastState = IsBlockActive(pos);

            if (Blocks.Remove(pos))
            {
                blockToChunk.Remove(pos);
                RebuildAllNetworks();
            }


            UpdateAllNetworks(lastState, false);
        }

        public void UpdateBlock((int x, int y, int z) pos)
        {
            if (Blocks.ContainsKey(pos)) RebuildAllNetworks();
            UpdateAllNetworks(false, false);
        }

        public void Rebuild()
        {
            RebuildAllNetworks();
            UpdateAllNetworks(false, true);
        }

        private bool IsNetworkActive(int id)
        {
            if (networks.TryGetValue(id, out List<(int x, int y, int z)> network))
            {
                int totalGen = 0;
                int totalCons = 0;
                for (int i = 0; i < network.Count; i++)
                {
                    if (Blocks.TryGetValue(network[i], out ElectricalBlock b))
                    {
                        if ((b.EFlags & ElectricalFlags.CanGenerate) != 0)
                            totalGen += b.GenerationRate;
                        if ((b.EFlags & ElectricalFlags.CanConsume) != 0)
                            totalCons += b.ConsumptionRate;
                    }
                }
                return totalGen >= totalCons;
            }
            return false;
        }


        public void UpdateAllNetworks(bool lastState, bool forseUpdate)
        {
            Debug.Log("COunt nets: " + networks.Count);

            foreach (var pair in networks)
            {
                int netId = pair.Key;
                List<(int x, int y, int z)> coords = pair.Value;
                if (!networkChunks.TryGetValue(netId, out HashSet<Chunk> chunks)) continue;

                bool newActive = IsNetworkActive(netId);
                bool changed;

                SetActiveState(coords, newActive);

                changed = forseUpdate ? true : lastState != newActive;

               // Debug.Log($"old state {lastState}  new {newActive} changed {changed} chunks {chunks.Count}");
               if(forseUpdate)
                {
                    Regenerate(chunks);
                    return;
                }
                if (changed && chunks.Count > 1) Regenerate(chunks);
            }
        }

        private bool SetActiveState(List<(int x, int y, int z)> coords, bool active)
        {
            bool changed = false;
            for (int i = 0; i < coords.Count; i++)
            {
                if (!Blocks.TryGetValue(coords[i], out ElectricalBlock block)) continue;
                bool old = block.IsActive;
                block.IsActive = active;
                if (block.IsActive != old) changed = true;
            }
            return changed;
        }

        public void RebuildAllNetworks()
        {
            networks.Clear();

            networkChunks.Clear();
            blockToNetwork.Clear();
            foreach (var kv in Blocks)
            {
                var coord = kv.Key;
                if (!blockToNetwork.ContainsKey(coord))
                {
                    int netId = CreateNetwork();
                    FloodFill(coord, netId);
                }
            }

        }

        private void FloodFill((int x, int y, int z) start, int netId)
        {
            Queue<(int, int, int)> queue = new Queue<(int, int, int)>();
            queue.Enqueue(start);
            if (!networkChunks.ContainsKey(netId))
                networkChunks[netId] = new HashSet<Chunk>();
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (blockToNetwork.ContainsKey(c)) continue;
                if (!Blocks.ContainsKey(c)) continue;
                blockToNetwork[c] = netId;
                if (!networks.ContainsKey(netId))
                    networks[netId] = new List<(int, int, int)>();
                networks[netId].Add(c);
                if (blockToChunk.TryGetValue(c, out Chunk ch))
                    networkChunks[netId].Add(ch);
                var neighbors = GetNeighbors(c);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    var n = neighbors[i];
                    if (Blocks.ContainsKey(n) && !blockToNetwork.ContainsKey(n))
                        queue.Enqueue(n);
                }
            }
        }

        private int CreateNetwork()
        {
            int id = nextNetworkId++;
            networks[id] = new List<(int, int, int)>();
            networkChunks[id] = new HashSet<Chunk>();

           
            return id;
        }

        private (int x, int y, int z)[] GetNeighbors((int x, int y, int z) p)
        {
            return new (int, int, int)[]
            {
                (p.x+1, p.y, p.z),
                (p.x-1, p.y, p.z),
                (p.x, p.y+1, p.z),
                (p.x, p.y-1, p.z),
                (p.x, p.y, p.z+1),
                (p.x, p.y, p.z-1)
            };
        }

        private void Regenerate(HashSet<Chunk> chunks)
        {
            foreach (var c in chunks) c.MarkNeedsRegenerate();
            Debug.Log("regen: " + chunks.Count);
        }
    }
}
