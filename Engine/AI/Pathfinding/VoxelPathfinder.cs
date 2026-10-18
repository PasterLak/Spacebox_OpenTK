using OpenTK.Mathematics;

namespace Engine.AI;

public sealed class VoxelPathfinder
{
    public const int DefaultMaxNodes = 2048;

    private static readonly Vector3i[] Offsets = BuildOffsets();
    private static readonly float[] Costs = BuildCosts(Offsets);
    private static readonly float Sqrt2 = MathF.Sqrt(2f);
    private static readonly float Sqrt3 = MathF.Sqrt(3f);

    private readonly List<Node> _nodes = new List<Node>(1024);
    private readonly Dictionary<Vector3i, int> _nodeIndex = new Dictionary<Vector3i, int>(1024);
    private readonly Dictionary<Vector3i, bool> _passable = new Dictionary<Vector3i, bool>(4096);
    private readonly PriorityQueue<int, float> _open = new PriorityQueue<int, float>(1024);
    private readonly List<Vector3> _points = new List<Vector3>(128);
    private INavigationGrid? _grid;

    public float HeuristicWeight { get; set; } = 1.2f;
    public bool AllowDiagonals { get; set; } = true;
    public int LastExpandedNodes { get; private set; }

    public PathStatus FindPath(INavigationGrid grid, Vector3 start, Vector3 goal, VoxelPath result, int maxNodes = DefaultMaxNodes)
    {
        result.Clear();
        Reset(grid);

        try
        {
            if (VoxelLine.IsClear(new CachedGrid(this), start, goal))
            {
                result.Add(goal);
                return PathStatus.Found;
            }

            return Search(start, goal, result, Math.Max(1, maxNodes));
        }
        finally
        {
            _grid = null;
        }
    }

    private PathStatus Search(Vector3 start, Vector3 goal, VoxelPath result, int maxNodes)
    {
        var startCell = VoxelLine.ToCell(start);
        var goalCell = VoxelLine.ToCell(goal);

        int startIndex = AddNode(startCell, -1, 0f);
        _open.Enqueue(startIndex, Heuristic(startCell, goalCell));

        int bestIndex = startIndex;
        float bestHeuristic = Heuristic(startCell, goalCell);
        int expanded = 0;

        while (_open.Count > 0 && expanded < maxNodes)
        {
            int currentIndex = _open.Dequeue();
            var current = _nodes[currentIndex];
            if (current.Closed) continue;

            current.Closed = true;
            _nodes[currentIndex] = current;
            expanded++;

            if (current.Cell == goalCell)
            {
                LastExpandedNodes = expanded;
                BuildPath(currentIndex, start, goal, result);
                return PathStatus.Found;
            }

            float heuristic = Heuristic(current.Cell, goalCell);
            if (heuristic < bestHeuristic)
            {
                bestHeuristic = heuristic;
                bestIndex = currentIndex;
            }

            Expand(currentIndex, current, goalCell);
        }

        LastExpandedNodes = expanded;
        if (bestIndex == startIndex) return PathStatus.Failed;

        BuildPath(bestIndex, start, VoxelLine.CellCenter(_nodes[bestIndex].Cell), result);
        return PathStatus.Partial;
    }

    private void Expand(int currentIndex, Node current, Vector3i goalCell)
    {
        int count = AllowDiagonals ? Offsets.Length : 6;

        for (int i = 0; i < count; i++)
        {
            var offset = Offsets[i];
            var next = current.Cell + offset;
            if (!CanMove(current.Cell, offset)) continue;

            float g = current.G + Costs[i];

            if (_nodeIndex.TryGetValue(next, out int nextIndex))
            {
                var node = _nodes[nextIndex];
                if (node.Closed || g >= node.G) continue;

                node.G = g;
                node.Parent = currentIndex;
                _nodes[nextIndex] = node;
            }
            else
            {
                nextIndex = AddNode(next, currentIndex, g);
            }

            _open.Enqueue(nextIndex, g + Heuristic(next, goalCell) * HeuristicWeight);
        }
    }

    private bool CanMove(Vector3i from, Vector3i offset)
    {
        if (!IsPassableCached(from + offset)) return false;

        int axes = (offset.X != 0 ? 1 : 0) | (offset.Y != 0 ? 2 : 0) | (offset.Z != 0 ? 4 : 0);
        if (axes == 1 || axes == 2 || axes == 4) return true;

        for (int mask = 1; mask < 7; mask++)
        {
            if ((mask & axes) != mask || mask == axes) continue;

            var corner = new Vector3i(
                (mask & 1) != 0 ? offset.X : 0,
                (mask & 2) != 0 ? offset.Y : 0,
                (mask & 4) != 0 ? offset.Z : 0);

            if (!IsPassableCached(from + corner)) return false;
        }

        return true;
    }

    private void BuildPath(int endIndex, Vector3 start, Vector3 end, VoxelPath result)
    {
        _points.Clear();
        _points.Add(end);

        int index = _nodes[endIndex].Parent;
        while (index > 0)
        {
            _points.Add(VoxelLine.CellCenter(_nodes[index].Cell));
            index = _nodes[index].Parent;
        }

        _points.Add(start);
        _points.Reverse();

        var grid = new CachedGrid(this);
        int anchor = 0;
        for (int i = 2; i < _points.Count; i++)
        {
            if (VoxelLine.IsClear(grid, _points[anchor], _points[i])) continue;

            anchor = i - 1;
            result.Add(_points[anchor]);
        }

        result.Add(_points[_points.Count - 1]);
    }

    private int AddNode(Vector3i cell, int parent, float g)
    {
        int index = _nodes.Count;
        _nodes.Add(new Node { Cell = cell, Parent = parent, G = g });
        _nodeIndex[cell] = index;
        return index;
    }

    private bool IsPassableCached(Vector3i cell)
    {
        if (_passable.TryGetValue(cell, out bool passable)) return passable;

        passable = _grid!.IsPassable(cell);
        _passable[cell] = passable;
        return passable;
    }

    private void Reset(INavigationGrid grid)
    {
        _grid = grid;
        _nodes.Clear();
        _nodeIndex.Clear();
        _passable.Clear();
        _open.Clear();
        LastExpandedNodes = 0;
    }

    private static float Heuristic(Vector3i a, Vector3i b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        int dz = Math.Abs(a.Z - b.Z);

        int min = Math.Min(dx, Math.Min(dy, dz));
        int max = Math.Max(dx, Math.Max(dy, dz));
        int mid = dx + dy + dz - min - max;

        return (Sqrt3 - Sqrt2) * min + (Sqrt2 - 1f) * mid + max;
    }

    private static Vector3i[] BuildOffsets()
    {
        var offsets = new List<Vector3i>(26)
        {
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
            new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            int axes = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
            if (axes >= 2) offsets.Add(new Vector3i(x, y, z));
        }

        return offsets.ToArray();
    }

    private static float[] BuildCosts(Vector3i[] offsets)
    {
        var costs = new float[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            var o = offsets[i];
            costs[i] = MathF.Sqrt(o.X * o.X + o.Y * o.Y + o.Z * o.Z);
        }
        return costs;
    }

    private struct Node
    {
        public Vector3i Cell;
        public int Parent;
        public float G;
        public bool Closed;
    }

    private readonly struct CachedGrid : INavigationGrid
    {
        private readonly VoxelPathfinder _owner;

        public CachedGrid(VoxelPathfinder owner)
        {
            _owner = owner;
        }

        public bool IsPassable(Vector3i cell) => _owner.IsPassableCached(cell);
    }
}
