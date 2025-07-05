using Engine;
using OpenTK.Mathematics;
using Spacebox.Scenes.Test;

public class SceneNode : Spacebox.Scenes.Test.Transform3D, INode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Transform";
    public string Tag { get; set; } = "Default";

    private SceneNode? _parent;
    public SceneNode? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;
            _parent?.Children.Remove(this);
            _parent = value;
            if (_parent != null && !_parent.Children.Contains(this))
                _parent.Children.Add(this);
            MarkDirtyRecursive();
        }
    }

    public bool HasParent => Parent is not null;
    public List<SceneNode> Children { get; } = new();
    public bool HasChildren => Children.Count > 0;
    public List<Spacebox.Scenes.Test.Component> Components { get; } = new();
    //public Transform3D Transform { get; } = new();

    public SceneNode()
    {
        Owner = this;
    }

    public void SetPosition(Vector3 pos)
    {
        Position = pos;
        MarkDirtyRecursive();
    }

    public void SetRotation(Quaternion rot)
    {
        Rotation = rot;
        MarkDirtyRecursive();
    }

    public void SetRotation(Vector3 eulerAngles)
    {
        Vector3 radians = new(
            MathHelper.DegreesToRadians(eulerAngles.X),
            MathHelper.DegreesToRadians(eulerAngles.Y),
            MathHelper.DegreesToRadians(eulerAngles.Z));
        Rotation = Quaternion.FromEulerAngles(radians);
        MarkDirtyRecursive();
    }

    public void SetScale(Vector3 scale)
    {
        Scale = scale;
        MarkDirtyRecursive();
    }

    public void SetScale(float scale)
    {
        SetScale(new Vector3(scale));
    }

    public void Move(Vector3 worldTranslation)
    {
        Vector3 worldPos = GetWorldPosition();
        Vector3 newWorldPos = worldPos + worldTranslation;
        if (Parent != null)
        {
            Matrix4 invParent = Matrix4.Invert(Parent.WorldMatrix);
            Position = Vector3.TransformPosition(newWorldPos, invParent);
        }
        else
        {
            Position = newWorldPos;
        }
        MarkDirtyRecursive();
    }

    public void Translate(Vector3 localTranslation)
    {
        Position += Rotation * localTranslation;
        MarkDirtyRecursive();
    }

    public void Rotate(Quaternion delta)
    {
        Rotation = delta * Rotation;
        MarkDirtyRecursive();
    }

    public void Rotate(Vector3 eulerAngles)
    {
        Vector3 radians = new(
            MathHelper.DegreesToRadians(eulerAngles.X),
            MathHelper.DegreesToRadians(eulerAngles.Y),
            MathHelper.DegreesToRadians(eulerAngles.Z));
        Rotate(Quaternion.FromEulerAngles(radians));
    }

    public void RotateAround(Vector3 point, Vector3 axis, float angleDegrees)
    {
        float angleRadians = MathHelper.DegreesToRadians(angleDegrees);
        Matrix4 rotationMatrix = Matrix4.CreateFromAxisAngle(axis.Normalized(), angleRadians);
        Vector3 direction = Position - point;
        direction = Vector3.TransformNormal(direction, rotationMatrix);
        Position = point + direction;
        Quaternion rot = Quaternion.FromAxisAngle(axis.Normalized(), angleRadians);
        Rotation = rot * Rotation;
        MarkDirtyRecursive();
    }

    public void ScaleBy(Vector3 scaleFactor)
    {
        Scale *= scaleFactor;
        MarkDirtyRecursive();
    }

    public void ScaleBy(float s)
    {
        ScaleBy(new Vector3(s));
    }

    //public Vector3 GetWorldPosition() => GetWorldPosition();
   // public Quaternion GetWorldRotation() => GetWorldRotation();

    public Vector3 WorldToLocal(Vector3 worldPoint)
    {
        Matrix4 invWorld = Matrix4.Invert(WorldMatrix);
        return Vector3.TransformPosition(worldPoint, invWorld);
    }

    public Vector3 LocalToWorld(Vector3 localPoint)
    {
        return Vector3.TransformPosition(localPoint, WorldMatrix);
    }

    public Vector3 GetLocalAxisPoint(Vector3 localAxis, float distance)
    {
        Vector3 localPoint = Vector3.Normalize(localAxis) * distance;
        return LocalToWorld(localPoint);
    }

    public void AddChild(SceneNode child)
    {
        if (child == null || child == this || child.Parent == this) return;
        child.Parent = this;
    }

    public void RemoveChild(SceneNode child)
    {
        if (child == null) return;
        if (Children.Remove(child))
        {
            child._parent = null;
            child.MarkDirtyRecursive();
        }
    }

    public void AttachComponent(Spacebox.Scenes.Test.Component component)
    {
        if (component == null) return;
        component.SetOwner(this);
        component.OnAttached();
        Components.Add(component);
    }

    public void DetachComponent(Spacebox.Scenes.Test.Component component)
    {
        if (component == null) return;
        if (Components.Remove(component))
            component.OnDettached();
    }

    public void Update()
    {
        var _ = WorldMatrix;
        foreach (var component in Components)
            if (component.Enabled)
                component.Update();
        foreach (var child in Children)
            child.Update();
    }

    public void UpdateAll()
    {
        if (this is IGameComponent component)
            component.Update();
        foreach (var child in Children)
            child.UpdateAll();
    }

    public void Render()
    {
        foreach (var component in Components)
            if (component.Enabled)
                component.Render();
        foreach (var child in Children)
            child.Render();
    }

    public void RenderAll(Camera camera)
    {
        if (this is IGameComponent component)
            component.Render();
        foreach (var child in Children)
            child.RenderAll(camera);
    }

    public void Traverse(Action<SceneNode> action)
    {
        action(this);
        foreach (var child in Children)
            child.Traverse(action);
    }

    public SceneNode? FindChild(Predicate<SceneNode> predicate)
    {
        foreach (var child in Children)
        {
            if (predicate(child))
                return child;
            var found = child.FindChild(predicate);
            if (found != null)
                return found;
        }
        return null;
    }

    public SceneNode? FindChildByName(string name)
    {
        return FindChild(node => node.Name == name);
    }

    public List<SceneNode> FindAllChildrenByName(string name)
    {
        List<SceneNode> found = new();
        Traverse(node => { if (node.Name == name) found.Add(node); });
        return found;
    }

    public List<SceneNode> FindAllChildrenByTag(string tag)
    {
        List<SceneNode> found = new();
        Traverse(node => { if (node.Tag == tag) found.Add(node); });
        return found;
    }

    public void ResetTransform()
    {
        Reset();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SceneNode);
    }

    public bool Equals(SceneNode other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SceneNode left, SceneNode right)
    {
        if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(SceneNode left, SceneNode right)
    {
        return !(left == right);
    }
}
