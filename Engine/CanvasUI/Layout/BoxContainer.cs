using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public enum BoxAlignment
{
    Begin,
    Center,
    End
}

public class BoxContainer : Container
{
    private readonly List<Control> _items = new List<Control>();
    private float[] _sizes = Array.Empty<float>();
    private bool[] _expanding = Array.Empty<bool>();

    public bool Vertical { get; }
    public BoxAlignment Alignment { get; set; } = BoxAlignment.Begin;

    public BoxContainer(bool vertical)
    {
        Vertical = vertical;
    }

    public float Separation => GetThemeConstant("separation", 4f);

    public override Vector2 GetMinimumSize()
    {
        float main = 0f, cross = 0f;
        int count = 0;
        var children = Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is not Control child || !child.Visible) continue;
            var min = child.GetCombinedMinimumSize();
            main += Vertical ? min.Y : min.X;
            cross = MathF.Max(cross, Vertical ? min.X : min.Y);
            count++;
        }
        if (count > 1) main += Separation * (count - 1);
        return Vertical ? new Vector2(cross, main) : new Vector2(main, cross);
    }

    protected override void LayoutChildren()
    {
        CollectItems();
        int count = _items.Count;
        if (count == 0) return;

        float separation = Separation;
        float available = (Vertical ? Size.Y : Size.X) - separation * (count - 1);
        float used = ResolveSizes(available);

        float offset = 0f;
        if (!AnyExpanding())
        {
            float free = MathF.Max(0f, available - used);
            offset = Alignment == BoxAlignment.Center ? free * 0.5f : Alignment == BoxAlignment.End ? free : 0f;
        }

        float cursor = offset;
        for (int i = 0; i < count; i++)
        {
            float size = _sizes[i];
            var rect = Vertical
                ? new Rect2(Rect.X, Rect.Y + cursor, Size.X, size)
                : new Rect2(Rect.X + cursor, Rect.Y, size, Size.Y);
            FitChildInRect(_items[i], rect);
            cursor += size + separation;
        }
    }

    private void CollectItems()
    {
        _items.Clear();
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control child && child.Visible) _items.Add(child);

        if (_sizes.Length < _items.Count)
        {
            _sizes = new float[_items.Count];
            _expanding = new bool[_items.Count];
        }
    }

    private float ResolveSizes(float available)
    {
        int count = _items.Count;
        float fixedSum = 0f;
        float ratioSum = 0f;
        for (int i = 0; i < count; i++)
        {
            var child = _items[i];
            var min = child.GetCombinedMinimumSize();
            _sizes[i] = Vertical ? min.Y : min.X;
            var flags = Vertical ? child.SizeFlagsVertical : child.SizeFlagsHorizontal;
            _expanding[i] = (flags & SizeFlags.Expand) != 0;
            if (_expanding[i]) ratioSum += MathF.Max(0.0001f, child.SizeFlagsStretchRatio);
            else fixedSum += _sizes[i];
        }

        bool changed = true;
        while (changed && ratioSum > 0f)
        {
            changed = false;
            float share = available - fixedSum;
            for (int i = 0; i < count; i++)
            {
                if (!_expanding[i]) continue;
                float ratio = MathF.Max(0.0001f, _items[i].SizeFlagsStretchRatio);
                float size = share * ratio / ratioSum;
                if (size >= _sizes[i]) continue;
                _expanding[i] = false;
                fixedSum += _sizes[i];
                ratioSum -= ratio;
                changed = true;
            }
        }

        float total = 0f;
        float shareFinal = available - fixedSum;
        for (int i = 0; i < count; i++)
        {
            if (_expanding[i]) _sizes[i] = MathF.Floor(shareFinal * MathF.Max(0.0001f, _items[i].SizeFlagsStretchRatio) / ratioSum);
            total += _sizes[i];
        }
        return total;
    }

    private bool AnyExpanding()
    {
        for (int i = 0; i < _items.Count; i++)
            if (_expanding[i]) return true;
        return false;
    }
}

public class HBoxContainer : BoxContainer
{
    public HBoxContainer() : base(false)
    {
    }
}

public class VBoxContainer : BoxContainer
{
    public VBoxContainer() : base(true)
    {
    }
}
