using OpenTK.Mathematics;

namespace Spacebox.Game.Generation;

public sealed class CellReservations
{
    private readonly object _sync = new();
    private readonly Dictionary<object, Vector3i[]> _owners = new();
    private readonly Dictionary<Vector3i, int> _counts = new();

    public void Set(object owner, IReadOnlyList<Vector3i> cells)
    {
        lock (_sync)
        {
            RemoveInternal(owner);
            if (cells.Count == 0) return;

            var copy = new Vector3i[cells.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = cells[i];
                _counts[copy[i]] = _counts.GetValueOrDefault(copy[i]) + 1;
            }
            _owners[owner] = copy;
        }
    }

    public void Remove(object owner)
    {
        lock (_sync) RemoveInternal(owner);
    }

    public bool IsReserved(Vector3i cell)
    {
        lock (_sync) return _counts.ContainsKey(cell);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _owners.Clear();
            _counts.Clear();
        }
    }

    private void RemoveInternal(object owner)
    {
        if (!_owners.Remove(owner, out var cells)) return;

        for (int i = 0; i < cells.Length; i++)
        {
            int count = _counts[cells[i]] - 1;
            if (count <= 0) _counts.Remove(cells[i]);
            else _counts[cells[i]] = count;
        }
    }
}
