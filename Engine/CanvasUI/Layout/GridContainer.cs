using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public class GridContainer : Container
{
    private readonly List<Control> _items = new List<Control>();
    private float[] _columnWidths = Array.Empty<float>();
    private float[] _rowHeights = Array.Empty<float>();
    private bool[] _columnExpand = Array.Empty<bool>();
    private bool[] _rowExpand = Array.Empty<bool>();
    private int _columns = 1;

    public int Columns
    {
        get => _columns;
        set
        {
            _columns = Math.Max(1, value);
            QueueLayout();
        }
    }

    public float HSeparation => GetThemeConstant("h_separation", 4f);
    public float VSeparation => GetThemeConstant("v_separation", 4f);

    public override Vector2 GetMinimumSize()
    {
        Measure();
        float w = Sum(_columnWidths, ColumnCount()) + HSeparation * Math.Max(0, ColumnCount() - 1);
        float h = Sum(_rowHeights, RowCount()) + VSeparation * Math.Max(0, RowCount() - 1);
        return new Vector2(w, h);
    }

    protected override void LayoutChildren()
    {
        Measure();
        int columns = ColumnCount();
        int rows = RowCount();
        if (columns == 0) return;

        Distribute(_columnWidths, _columnExpand, columns, Size.X - HSeparation * (columns - 1));
        Distribute(_rowHeights, _rowExpand, rows, Size.Y - VSeparation * (rows - 1));

        float y = Rect.Y;
        for (int row = 0; row < rows; row++)
        {
            float x = Rect.X;
            for (int column = 0; column < columns; column++)
            {
                int index = row * _columns + column;
                if (index >= _items.Count) break;
                FitChildInRect(_items[index], new Rect2(x, y, _columnWidths[column], _rowHeights[row]));
                x += _columnWidths[column] + HSeparation;
            }
            y += _rowHeights[row] + VSeparation;
        }
    }

    private int ColumnCount() => Math.Min(_columns, _items.Count);

    private int RowCount() => _items.Count == 0 ? 0 : (_items.Count + _columns - 1) / _columns;

    private void Measure()
    {
        _items.Clear();
        var children = Children;
        for (int i = 0; i < children.Count; i++)
            if (children[i] is Control child && child.Visible) _items.Add(child);

        int rows = RowCount();
        if (_columnWidths.Length < _columns) { _columnWidths = new float[_columns]; _columnExpand = new bool[_columns]; }
        if (_rowHeights.Length < rows) { _rowHeights = new float[rows]; _rowExpand = new bool[rows]; }
        Array.Clear(_columnWidths);
        Array.Clear(_columnExpand);
        Array.Clear(_rowHeights);
        Array.Clear(_rowExpand);

        for (int i = 0; i < _items.Count; i++)
        {
            int column = i % _columns;
            int row = i / _columns;
            var min = _items[i].GetCombinedMinimumSize();
            _columnWidths[column] = MathF.Max(_columnWidths[column], min.X);
            _rowHeights[row] = MathF.Max(_rowHeights[row], min.Y);
            if ((_items[i].SizeFlagsHorizontal & SizeFlags.Expand) != 0) _columnExpand[column] = true;
            if ((_items[i].SizeFlagsVertical & SizeFlags.Expand) != 0) _rowExpand[row] = true;
        }
    }

    private static void Distribute(float[] sizes, bool[] expand, int count, float available)
    {
        float used = Sum(sizes, count);
        int expanders = 0;
        for (int i = 0; i < count; i++) if (expand[i]) expanders++;
        if (expanders == 0 || available <= used) return;

        float extra = (available - used) / expanders;
        for (int i = 0; i < count; i++) if (expand[i]) sizes[i] = MathF.Floor(sizes[i] + extra);
    }

    private static float Sum(float[] values, int count)
    {
        float sum = 0f;
        for (int i = 0; i < count; i++) sum += values[i];
        return sum;
    }
}
