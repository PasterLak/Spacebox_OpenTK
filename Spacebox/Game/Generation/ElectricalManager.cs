using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation
{
    public class ElectricNetworkManager
    {
        public Dictionary<(int x, int y, int z), ElectricalBlock> Blocks = new Dictionary<(int x, int y, int z), ElectricalBlock>();
        Dictionary<int, List<(int x, int y, int z)>> networks = new Dictionary<int, List<(int, int, int)>>();
        Dictionary<(int x, int y, int z), int> blockToNetwork = new Dictionary<(int x, int y, int z), int>();
        Dictionary<int, HashSet<Chunk>> networkChunks = new Dictionary<int, HashSet<Chunk>>();
        Dictionary<(int x, int y, int z), Chunk> blockToChunk = new Dictionary<(int x, int y, int z), Chunk>();

        Dictionary<int, bool> networkActiveCache = new Dictionary<int, bool>();
        HashSet<(int x, int y, int z)> tempAffectedBlocks = new HashSet<(int x, int y, int z)>();
        Dictionary<(int x, int y, int z), bool> tempOldStates = new Dictionary<(int x, int y, int z), bool>();
        HashSet<Chunk> tempChangedChunks = new HashSet<Chunk>();
        Queue<(int, int, int)> floodFillQueue = new Queue<(int, int, int)>();

        int nextNetworkId = 1;

        

        public void AddBlockFast((int x, int y, int z) globalPos, ElectricalBlock block, Chunk parentChunk)
        {
            Blocks[globalPos] = block;
            blockToChunk[globalPos] = parentChunk;
        }

        public void AddBlock((int x, int y, int z) globalPos, ElectricalBlock block, Chunk parentChunk)
        {
            if (block.EFlags == ElectricalFlags.None) return;

            GetPotentiallyAffectedBlocks(globalPos);
            SaveBlockStates();

            Blocks[globalPos] = block;
            blockToChunk[globalPos] = parentChunk;

            RebuildAllNetworks();
            UpdateChangedChunks();
        }

        public void RemoveBlock(Chunk chunk, Vector3Byte posInChunk)
        {
            var index = chunk.PositionIndex;
            Vector3 entityLocalPos = SpaceEntity.ChunkIndexToLocal(index);
            var globalPos = ((int)(entityLocalPos.X + posInChunk.X),
                           (int)(entityLocalPos.Y + posInChunk.Y),
                           (int)(entityLocalPos.Z + posInChunk.Z));
            RemoveBlock(globalPos);
        }

        public void RemoveBlock((int x, int y, int z) globalPos)
        {
            if (!Blocks.ContainsKey(globalPos)) return;

            GetNetworkBlocks(globalPos);
            SaveBlockStates();

            Blocks.Remove(globalPos);
            blockToChunk.Remove(globalPos);

            RebuildAllNetworks();
            UpdateChangedChunks();
        }

        public void UpdateBlock((int x, int y, int z) globalPos)
        {
            if (!Blocks.ContainsKey(globalPos)) return;

            GetNetworkBlocks(globalPos);
            SaveBlockStates();

            RebuildAllNetworks();
            UpdateChangedChunks();
        }

        public void Rebuild()
        {
            RebuildAllNetworks();
            UpdateAllNetworksForced();
        }

        private void GetPotentiallyAffectedBlocks((int x, int y, int z) globalPos)
        {
            tempAffectedBlocks.Clear();
            tempAffectedBlocks.Add(globalPos);

            var n1 = (globalPos.x + 1, globalPos.y, globalPos.z);
            var n2 = (globalPos.x - 1, globalPos.y, globalPos.z);
            var n3 = (globalPos.x, globalPos.y + 1, globalPos.z);
            var n4 = (globalPos.x, globalPos.y - 1, globalPos.z);
            var n5 = (globalPos.x, globalPos.y, globalPos.z + 1);
            var n6 = (globalPos.x, globalPos.y, globalPos.z - 1);

            CheckNeighbor(n1);
            CheckNeighbor(n2);
            CheckNeighbor(n3);
            CheckNeighbor(n4);
            CheckNeighbor(n5);
            CheckNeighbor(n6);
        }

        private void CheckNeighbor((int x, int y, int z) neighbor)
        {
            if (Blocks.ContainsKey(neighbor))
            {
                if (blockToNetwork.TryGetValue(neighbor, out int netId))
                {
                    if (networks.TryGetValue(netId, out var netBlocks))
                    {
                        foreach (var block in netBlocks)
                            tempAffectedBlocks.Add(block);
                    }
                }
                else
                {
                    tempAffectedBlocks.Add(neighbor);
                }
            }
        }

        private void GetNetworkBlocks((int x, int y, int z) globalPos)
        {
            tempAffectedBlocks.Clear();

            if (blockToNetwork.TryGetValue(globalPos, out int netId))
            {
                if (networks.TryGetValue(netId, out var netBlocks))
                {
                    foreach (var block in netBlocks)
                        tempAffectedBlocks.Add(block);
                }
            }
            else
            {
                tempAffectedBlocks.Add(globalPos);
            }
        }

        private void SaveBlockStates()
        {
            tempOldStates.Clear();
            foreach (var pos in tempAffectedBlocks)
            {
                if (Blocks.TryGetValue(pos, out var block))
                {
                    tempOldStates[pos] = block.IsActive;
                }
            }
        }

        private void UpdateChangedChunks()
        {
            tempChangedChunks.Clear();

            foreach (var oldState in tempOldStates)
            {
                var pos = oldState.Key;
                bool oldActive = oldState.Value;
                bool newActive = false;

                if (Blocks.TryGetValue(pos, out var block))
                {
                    if (blockToNetwork.TryGetValue(pos, out int netId))
                    {
                        if (!networkActiveCache.TryGetValue(netId, out newActive))
                        {
                            newActive = IsNetworkActive(netId);
                            networkActiveCache[netId] = newActive;
                        }
                        block.IsActive = newActive;
                    }
                    else
                    {
                        block.IsActive = false;
                    }

                    if (oldActive != block.IsActive)
                    {
                        if (blockToChunk.TryGetValue(pos, out Chunk chunk))
                        {
                            tempChangedChunks.Add(chunk);
                        }
                    }
                }
            }

            foreach (var network in networks)
            {
                if (!networkActiveCache.TryGetValue(network.Key, out bool isActive))
                {
                    isActive = IsNetworkActive(network.Key);
                    networkActiveCache[network.Key] = isActive;
                }

                foreach (var pos in network.Value)
                {
                    if (!tempOldStates.ContainsKey(pos) && Blocks.TryGetValue(pos, out var block))
                    {
                        if (block.IsActive != isActive)
                        {
                            block.IsActive = isActive;
                            if (blockToChunk.TryGetValue(pos, out Chunk chunk))
                            {
                                tempChangedChunks.Add(chunk);
                            }
                        }
                    }
                }
            }
           // Debug.Log("Regen: " + tempChangedChunks.Count);
            if (tempChangedChunks.Count > 0)
            {
                foreach (var c in tempChangedChunks)
                {
                    if (c != null)
                        c.NeedsToRegenerateMesh = true;
                }
            }
        }

        private void UpdateAllNetworksForced()
        {
            tempChangedChunks.Clear();

            foreach (var network in networks)
            {
                int netId = network.Key;
                bool isActive = IsNetworkActive(netId);

                foreach (var blockPos in network.Value)
                {
                    if (Blocks.TryGetValue(blockPos, out ElectricalBlock block))
                    {
                        block.IsActive = isActive;
                        if (blockToChunk.TryGetValue(blockPos, out Chunk chunk))
                        {
                            tempChangedChunks.Add(chunk);
                        }
                    }
                }
            }

            foreach (var kv in Blocks)
            {
                if (!blockToNetwork.ContainsKey(kv.Key))
                {
                    kv.Value.IsActive = false;
                    if (blockToChunk.TryGetValue(kv.Key, out Chunk chunk))
                    {
                        tempChangedChunks.Add(chunk);
                    }
                }
            }
           // Debug.Log("Regen: " + tempChangedChunks.Count);
            if (tempChangedChunks.Count > 0)
            {

                foreach (var c in tempChangedChunks)
                {
                    if (c != null)
                        c.NeedsToRegenerateMesh = true;
                }
            }
        }

        private bool IsNetworkActive(int id)
        {
            if (networks.TryGetValue(id, out List<(int x, int y, int z)> network))
            {
                int totalGen = 0;
                int totalCons = 0;

                int count = network.Count;
                for (int i = 0; i < count; i++)
                {
                    if (Blocks.TryGetValue(network[i], out ElectricalBlock b))
                    {
                        var flags = b.EFlags;
                        if ((flags & ElectricalFlags.CanGenerate) != 0)
                            totalGen += b.GenerationRate;
                        if ((flags & ElectricalFlags.CanConsume) != 0)
                            totalCons += b.ConsumptionRate;
                    }
                }

                return totalGen >= totalCons;
            }
            return false;
        }

        public void RebuildAllNetworks()
        {
            networks.Clear();
            networkChunks.Clear();
            blockToNetwork.Clear();
            networkActiveCache.Clear();
            nextNetworkId = 1;

            foreach (var kv in Blocks)
            {
                var coord = kv.Key;
                if (!blockToNetwork.ContainsKey(coord))
                {
                    int netId = nextNetworkId++;
                    networks[netId] = new List<(int, int, int)>();
                    networkChunks[netId] = new HashSet<Chunk>();
                    FloodFill(coord, netId);
                }
            }
        }

        private void FloodFill((int x, int y, int z) start, int netId)
        {
            floodFillQueue.Clear();
            floodFillQueue.Enqueue(start);

            var netList = networks[netId];
            var netChunks = networkChunks[netId];

            while (floodFillQueue.Count > 0)
            {
                var (x,y,z) = floodFillQueue.Dequeue();

                if (blockToNetwork.ContainsKey((x, y, z))) continue;
                if (!Blocks.ContainsKey((x, y, z))) continue;

                blockToNetwork[(x, y, z)] = netId;
                netList.Add((x, y, z));

                if (blockToChunk.TryGetValue((x, y, z), out Chunk ch))
                    netChunks.Add(ch);

                var n1 = (x + 1, y, z);
                var n2 = (x - 1, y, z);
                var n3 = (x, y + 1, z);
                var n4 = (x, y - 1, z);
                var n5 = (x, y, z + 1);
                var n6 = (x, y, z - 1);

                if (Blocks.ContainsKey(n1) && !blockToNetwork.ContainsKey(n1)) floodFillQueue.Enqueue(n1);
                if (Blocks.ContainsKey(n2) && !blockToNetwork.ContainsKey(n2)) floodFillQueue.Enqueue(n2);
                if (Blocks.ContainsKey(n3) && !blockToNetwork.ContainsKey(n3)) floodFillQueue.Enqueue(n3);
                if (Blocks.ContainsKey(n4) && !blockToNetwork.ContainsKey(n4)) floodFillQueue.Enqueue(n4);
                if (Blocks.ContainsKey(n5) && !blockToNetwork.ContainsKey(n5)) floodFillQueue.Enqueue(n5);
                if (Blocks.ContainsKey(n6) && !blockToNetwork.ContainsKey(n6)) floodFillQueue.Enqueue(n6);
            }
        }


        public int GetNetworkCurrentPower(int networkId)
        {
            if (!networks.TryGetValue(networkId, out var networkBlocks))
                return 0;

            int totalPower = 0;
            int count = networkBlocks.Count;

            for (int i = 0; i < count; i++)
            {
                if (Blocks.TryGetValue(networkBlocks[i], out ElectricalBlock block))
                {
                    totalPower += block.CurrentPower;
                }
            }

            return totalPower;
        }

        public int GetNetworkId((int x, int y, int z) blockPos)
        {
            if (!blockToNetwork.TryGetValue(blockPos, out int networkId))
            {

                return 0;
            }
            return networkId;
        }
        public int GetNetworkCurrentPowerByBlock((int x, int y, int z) blockPos)
        {
            if (!blockToNetwork.TryGetValue(blockPos, out int networkId))
            {
               
                return 0;
            }
                

            return GetNetworkCurrentPower(networkId);
        }

        public (int current, int max) GetNetworkPowerCapacity(int networkId)
        {
            if (!networks.TryGetValue(networkId, out var networkBlocks))
                return (0, 0);

            int currentPower = 0;
            int maxPower = 0;
            int count = networkBlocks.Count;

            for (int i = 0; i < count; i++)
            {
                if (Blocks.TryGetValue(networkBlocks[i], out ElectricalBlock block))
                {
                    currentPower += block.CurrentPower;
                    maxPower += block.MaxPower;
                }
            }

            return (currentPower, maxPower);
        }

        public (int generation, int consumption) GetNetworkPowerFlow(int networkId)
        {
            if (!networks.TryGetValue(networkId, out var networkBlocks))
                return (0, 0);

            int totalGen = 0;
            int totalCons = 0;
            int count = networkBlocks.Count;

            for (int i = 0; i < count; i++)
            {
                if (Blocks.TryGetValue(networkBlocks[i], out ElectricalBlock block))
                {
                    var flags = block.EFlags;
                    if ((flags & ElectricalFlags.CanGenerate) != 0)
                        totalGen += block.GenerationRate;
                    if ((flags & ElectricalFlags.CanConsume) != 0)
                        totalCons += block.ConsumptionRate;
                }
            }

            return (totalGen, totalCons);
        }

        public Dictionary<int, (int current, int max, int generation, int consumption)> GetAllNetworksInfo()
        {
            var result = new Dictionary<int, (int, int, int, int)>();

            foreach (var network in networks)
            {
                int networkId = network.Key;
                int currentPower = 0;
                int maxPower = 0;
                int totalGen = 0;
                int totalCons = 0;

                foreach (var blockPos in network.Value)
                {
                    if (Blocks.TryGetValue(blockPos, out ElectricalBlock block))
                    {
                        currentPower += block.CurrentPower;
                        maxPower += block.MaxPower;

                        var flags = block.EFlags;
                        if ((flags & ElectricalFlags.CanGenerate) != 0)
                            totalGen += block.GenerationRate;
                        if ((flags & ElectricalFlags.CanConsume) != 0)
                            totalCons += block.ConsumptionRate;
                    }
                }

                result[networkId] = (currentPower, maxPower, totalGen, totalCons);
            }

            return result;
        }
    }
}