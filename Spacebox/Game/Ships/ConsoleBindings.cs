using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public sealed class ConsoleBindings
{
    public static readonly Color4[] GroupColors =
    {
        new Color4(1f, 0.15f, 0.15f, 1f),
        new Color4(0.2f, 0.9f, 0.2f, 1f),
        new Color4(0.55f, 0.8f, 1f, 1f),
        new Color4(1f, 0.95f, 0.2f, 1f),
        new Color4(0.85f, 0.5f, 1f, 1f),
        new Color4(1f, 0.6f, 0.1f, 1f),
        new Color4(0.6f, 0.4f, 0.2f, 1f),
        new Color4(0.25f, 0.9f, 0.8f, 1f),
        new Color4(1f, 0.6f, 0.75f, 1f),
        new Color4(0.15f, 0.25f, 0.75f, 1f),
        new Color4(0.7f, 0.7f, 0.7f, 1f),
        new Color4(0.5f, 0.9f, 0.5f, 1f),
        new Color4(0.9f, 0.3f, 0.6f, 1f),
        new Color4(0.4f, 0.6f, 0.2f, 1f),
        new Color4(0.3f, 0.3f, 0.3f, 1f),
        new Color4(1f, 1f, 1f, 1f)
    };

    private readonly int[] _keys;
    private readonly Dictionary<Vector3i, byte> _bound = new Dictionary<Vector3i, byte>();

    public int GroupCount => _keys.Length;
    public bool Weapons { get; }
    public Vector3i? Camera { get; private set; }
    public IReadOnlyDictionary<Vector3i, byte> Bound => _bound;
    public int Version { get; private set; }

    public ConsoleBindings(int groupCount, bool weapons)
    {
        _keys = new int[Math.Clamp(groupCount, 1, GroupColors.Length)];
        Weapons = weapons;
        for (int i = 0; i < _keys.Length; i++)
            _keys[i] = ShipKeys.DefaultKey(i, weapons);
    }

    public int GetKey(int group) => group >= 0 && group < _keys.Length ? _keys[group] : ShipKeys.None;

    public void SetKey(int group, int key)
    {
        if (group < 0 || group >= _keys.Length || _keys[group] == key) return;
        _keys[group] = key;
        Version++;
    }

    public int GetGroup(Vector3i cell) => _bound.TryGetValue(cell, out var group) ? group : -1;

    public int Cycle(Vector3i cell)
    {
        int next = GetGroup(cell) + 1;
        if (next >= _keys.Length)
        {
            Unbind(cell);
            return -1;
        }

        Bind(cell, next);
        return next;
    }

    public void Bind(Vector3i cell, int group)
    {
        if (group < 0 || group >= _keys.Length) return;
        if (_bound.TryGetValue(cell, out var current) && current == group) return;

        _bound[cell] = (byte)group;
        Version++;
    }

    public bool Unbind(Vector3i cell)
    {
        bool removed = _bound.Remove(cell);
        if (Camera == cell)
        {
            Camera = null;
            removed = true;
        }

        if (removed) Version++;
        return removed;
    }

    public void ToggleCamera(Vector3i cell)
    {
        Camera = Camera == cell ? null : cell;
        Version++;
    }

    public void SetCamera(Vector3i? cell)
    {
        if (Camera == cell) return;
        Camera = cell;
        Version++;
    }

    public bool IsGroupPressed(int group, ushort buttons) => group >= 0 && group < 16 && (buttons & (1 << group)) != 0;

    public ushort ReadButtons(Func<int, bool> isHeld)
    {
        ushort buttons = 0;
        for (int i = 0; i < _keys.Length; i++)
        {
            if (isHeld(_keys[i])) buttons |= (ushort)(1 << i);
        }
        return buttons;
    }
}
