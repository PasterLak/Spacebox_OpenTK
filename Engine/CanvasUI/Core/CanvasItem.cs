using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public abstract class CanvasItem
{
    private readonly List<CanvasItem> _children = new List<CanvasItem>();
    private bool _visible = true;

    public string Name { get; set; } = "";
    public CanvasItem Parent { get; private set; }
    public IReadOnlyList<CanvasItem> Children => _children;
    public Color4 Modulate { get; set; } = Color4.White;
    public Color4 SelfModulate { get; set; } = Color4.White;
    public CanvasRoot Root { get; private set; }

    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value) return;
            _visible = value;
            QueueLayout();
        }
    }

    internal void SetVisibleSilently(bool visible) => _visible = visible;

    public bool IsVisibleInTree
    {
        get
        {
            for (var item = this; item != null; item = item.Parent)
                if (!item._visible) return false;
            return true;
        }
    }

    public abstract Transform2D GlobalTransform { get; }

    public T AddChild<T>(T child) where T : CanvasItem
    {
        if (child == null || child == this) return child;
        child.Parent?.RemoveChild(child);
        _children.Add(child);
        child.Parent = this;
        child.SetRoot(Root);
        QueueLayout();
        return child;
    }

    public T InsertChild<T>(int index, T child) where T : CanvasItem
    {
        if (child == null || child == this) return child;
        child.Parent?.RemoveChild(child);
        _children.Insert(Math.Clamp(index, 0, _children.Count), child);
        child.Parent = this;
        child.SetRoot(Root);
        QueueLayout();
        return child;
    }

    public int IndexOf(CanvasItem child) => _children.IndexOf(child);

    public void RemoveChild(CanvasItem child)
    {
        if (child == null || child.Parent != this) return;
        _children.Remove(child);
        child.Parent = null;
        child.SetRoot(null);
        QueueLayout();
    }

    public void ClearChildren()
    {
        for (int i = _children.Count - 1; i >= 0; i--) RemoveChild(_children[i]);
    }

    public void MoveToFront()
    {
        if (Parent == null) return;
        Parent._children.Remove(this);
        Parent._children.Add(this);
    }

    public T FindChild<T>(string name) where T : CanvasItem
    {
        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i];
            if (child.Name == name && child is T match) return match;
            var nested = child.FindChild<T>(name);
            if (nested != null) return nested;
        }
        return null;
    }

    public void QueueLayout()
    {
        Root?.MarkLayoutDirty();
    }

    public virtual void Process(float deltaTime)
    {
    }

    protected virtual void Draw(ICanvasRenderer renderer)
    {
    }

    protected virtual void OnEnterTree()
    {
    }

    protected virtual void OnExitTree()
    {
    }

    internal void ProcessTree(float deltaTime)
    {
        if (!_visible) return;
        Process(deltaTime);
        for (int i = 0; i < _children.Count; i++) _children[i].ProcessTree(deltaTime);
    }

    internal virtual void DrawTree(ICanvasRenderer renderer, Color4 parentModulate)
    {
        if (!_visible) return;

        var modulate = Multiply(parentModulate, Modulate);
        renderer.Transform = GlobalTransform;
        renderer.Modulate = Multiply(modulate, SelfModulate);
        Draw(renderer);
        DrawChildren(renderer, modulate);
    }

    protected void DrawChildren(ICanvasRenderer renderer, Color4 modulate)
    {
        for (int i = 0; i < _children.Count; i++) _children[i].DrawTree(renderer, modulate);
    }

    internal void SetRoot(CanvasRoot root)
    {
        if (Root == root) return;
        if (Root != null) OnExitTree();
        Root = root;
        if (Root != null) OnEnterTree();
        for (int i = 0; i < _children.Count; i++) _children[i].SetRoot(root);
    }

    internal static Color4 Multiply(Color4 a, Color4 b) => new Color4(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
}
