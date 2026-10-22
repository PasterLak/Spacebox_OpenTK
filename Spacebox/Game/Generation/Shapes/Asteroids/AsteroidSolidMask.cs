using System.Collections;

namespace Spacebox.Game.Generation.Shapes.Asteroids;

public sealed class AsteroidSolidMask
{
    private readonly BitArray _solid;

    public AsteroidSolidMask(int origin, int size)
    {
        Origin = origin;
        Size = size;
        _solid = new BitArray(size * size * size);
    }

    public int Origin { get; }
    public int Size { get; }

    public void Set(int x, int y, int z, bool solid)
    {
        if (TryIndex(x, y, z, out int index)) _solid[index] = solid;
    }

    public bool IsSolid(int x, int y, int z)
    {
        return TryIndex(x, y, z, out int index) && _solid[index];
    }

    public int KeepLargestComponent()
    {
        int total = Size * Size * Size;
        var visited = new BitArray(total);
        var queue = new int[Math.Max(1, total)];
        int bestStart = -1;
        int bestSize = 0;

        for (int i = 0; i < total; i++)
        {
            if (!_solid[i] || visited[i]) continue;

            int size = Flood(i, visited, queue, null);
            if (size > bestSize)
            {
                bestSize = size;
                bestStart = i;
            }
        }

        var keep = new BitArray(total);
        if (bestStart >= 0) Flood(bestStart, new BitArray(total), queue, keep);

        int removed = 0;
        for (int i = 0; i < total; i++)
        {
            if (_solid[i] && !keep[i]) removed++;
        }

        _solid.SetAll(false);
        _solid.Or(keep);
        return removed;
    }

    private int Flood(int start, BitArray visited, int[] queue, BitArray mark)
    {
        int head = 0;
        int tail = 0;
        queue[tail++] = start;
        visited[start] = true;
        int plane = Size * Size;

        while (head < tail)
        {
            int index = queue[head++];
            mark?.Set(index, true);

            int x = index % Size;
            int y = index / Size % Size;
            int z = index / plane;

            if (x > 0) Visit(index - 1);
            if (x < Size - 1) Visit(index + 1);
            if (y > 0) Visit(index - Size);
            if (y < Size - 1) Visit(index + Size);
            if (z > 0) Visit(index - plane);
            if (z < Size - 1) Visit(index + plane);
        }

        return tail;

        void Visit(int next)
        {
            if (visited[next] || !_solid[next]) return;
            visited[next] = true;
            queue[tail++] = next;
        }
    }

    private bool TryIndex(int x, int y, int z, out int index)
    {
        int lx = x - Origin;
        int ly = y - Origin;
        int lz = z - Origin;
        if (lx < 0 || ly < 0 || lz < 0 || lx >= Size || ly >= Size || lz >= Size)
        {
            index = -1;
            return false;
        }

        index = lx + ly * Size + lz * Size * Size;
        return true;
    }
}
