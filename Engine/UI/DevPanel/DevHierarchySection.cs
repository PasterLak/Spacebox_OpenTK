using Engine.CanvasUI;
using Engine.SceneManagement;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevHierarchySection : DevPanelSection
{
    private const int MaxRows = 2000;

    private readonly ItemList _tree = new ItemList { CustomMinimumSize = new Vector2(0f, 240f), AllowReselect = true };
    private readonly Action<Node3D> _select;
    private readonly List<Node3D> _nodes = new List<Node3D>();
    private readonly List<Node3D> _previous = new List<Node3D>();
    private Guid? _selectedId;

    public DevHierarchySection(Action<Node3D> select) : base("Hierarchy")
    {
        _select = select;
        Content.AddChild(_tree);
        _tree.ItemSelected += index =>
        {
            if (_tree.Items[index].Metadata is not Node3D node) return;
            _selectedId = node.Id;
            _select(node);
        };
    }

    public override void Refresh()
    {
        _nodes.Clear();
        var scene = SceneManager.Current;
        if (scene != null) Collect(scene);
        if (SameAsPrevious()) return;

        _previous.Clear();
        _previous.AddRange(_nodes);
        _tree.Clear();
        int selected = -1;
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            var item = _tree.AddItem(string.IsNullOrEmpty(node.Name) ? node.GetType().Name : node.Name, null, node);
            item.Indent = Depth(node);
            item.Secondary = node.Children.Count > 0 ? node.Children.Count.ToString() : "";
            if (!node.Enabled) item.TextColor = DarkTheme.TextDim;
            if (_selectedId == node.Id) selected = i;
        }
        if (selected >= 0) _tree.Select(selected);
    }

    private void Collect(Node3D node)
    {
        if (_nodes.Count >= MaxRows) return;
        _nodes.Add(node);
        foreach (var child in node.Children) Collect(child);
    }

    private bool SameAsPrevious()
    {
        if (_nodes.Count != _previous.Count) return false;
        for (int i = 0; i < _nodes.Count; i++)
            if (!ReferenceEquals(_nodes[i], _previous[i])) return false;
        return true;
    }

    private static int Depth(Node3D node)
    {
        int depth = 0;
        for (var parent = node.Parent; parent != null; parent = parent.Parent) depth++;
        return depth;
    }
}
