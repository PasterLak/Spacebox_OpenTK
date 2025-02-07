using Engine;

namespace Spacebox.Game.Generation
{
    public class ElectricNetworkManager
    {
        public Dictionary<(int x, int y, int z), ElectricalBlock> Blocks
            = new Dictionary<(int x, int y, int z), ElectricalBlock>();

        Dictionary<int, List<(int x, int y, int z)>> networks
            = new Dictionary<int, List<(int, int, int)>>();
        Dictionary<(int x, int y, int z), int> blockToNetwork
            = new Dictionary<(int x, int y, int z), int>();

        int nextNetworkId = 1;

        public void AddBlock((int x, int y, int z) pos, ElectricalBlock block)
        {
            Blocks[pos] = block;
            RebuildAllNetworks();
            UpdateAllNetworks();
        }

        public void RemoveBlock((int x, int y, int z) pos)
        {
            if (Blocks.Remove(pos))
            {
                RebuildAllNetworks();
            }
            UpdateAllNetworks();
        }

        public void UpdateBlock((int x, int y, int z) pos)
        {
            if (Blocks.ContainsKey(pos))
            {
                RebuildAllNetworks();
            }
            UpdateAllNetworks();
        }

        public void UpdateAllNetworks()
        {
            foreach (var net in networks)
            {
                var coords = net.Value;
                int totalGen = 0;
                int totalCons = 0;
                var eblocks = new List<ElectricalBlock>(coords.Count);

                for (int i = 0; i < coords.Count; i++)
                {
                    if (Blocks.TryGetValue(coords[i], out var blk))
                    {
                        eblocks.Add(blk);
                    }
                }

                for (int i = 0; i < eblocks.Count; i++)
                {
                    var b = eblocks[i];
                    if ((b.EFlags & ElectricalFlags.CanGenerate) != 0)
                    {
                        totalGen += b.GenerationRate;
                    }
                    if ((b.EFlags & ElectricalFlags.CanConsume) != 0)
                    {
                        totalCons += b.ConsumptionRate;
                    }
                }

                bool networkOn = totalGen >= totalCons;

                for (int i = 0; i < eblocks.Count; i++)
                {
                    var b = eblocks[i];
                    b.SetEnableEmission(networkOn);
                }
            }
        }

        public void RebuildAllNetworks()
        {
            networks.Clear();
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

        void FloodFill((int x, int y, int z) start, int netId)
        {
            var queue = new Queue<(int, int, int)>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (blockToNetwork.ContainsKey(c)) continue;
                if (!Blocks.ContainsKey(c)) continue;

                blockToNetwork[c] = netId;
                if (!networks.ContainsKey(netId))
                {
                    networks[netId] = new List<(int, int, int)>();
                }
                networks[netId].Add(c);

                var neighbors = GetNeighbors(c);
                for (int i = 0; i < neighbors.Length; i++)
                {
                    var n = neighbors[i];
                    if (Blocks.ContainsKey(n) && !blockToNetwork.ContainsKey(n))
                    {
                        queue.Enqueue(n);
                    }
                }
            }
        }

        int CreateNetwork()
        {
            int id = nextNetworkId++;
            networks[id] = new List<(int, int, int)>();
            return id;
        }

        (int x, int y, int z)[] GetNeighbors((int x, int y, int z) p)
        {
            return new (int x, int y, int z)[]
            {
                (p.x+1, p.y, p.z),
                (p.x-1, p.y, p.z),
                (p.x, p.y+1, p.z),
                (p.x, p.y-1, p.z),
                (p.x, p.y, p.z+1),
                (p.x, p.y, p.z-1)
            };
        }
    }
}
