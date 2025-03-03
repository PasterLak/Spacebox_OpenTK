using Engine;
using OpenTK.Mathematics;
using Spacebox.Scenes.Test;

public class SceneNode
{
    public Guid Id { get; } = Guid.NewGuid();

    private SceneNode? _parent;
    public SceneNode? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value)
                return;
            _parent?.Children.Remove(this);
            _parent = value;
            if (_parent != null && !_parent.Children.Contains(this))
                _parent.Children.Add(this);
            MarkDirty();
        }
    }

    public bool HasParent => Parent is not null;

    public string Name { get; set; } = "Transform";
    public string Tag { get; set; } = "Default";
    public List<SceneNode> Children { get; } = new List<SceneNode>();
    public bool HasChildren => Children.Count > 0;
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Scale { get; set; } = Vector3.One;
    public bool Resizable { get; protected set; } = true;
    private Matrix4 _localMatrix = Matrix4.Identity;
    private Matrix4 _worldMatrix = Matrix4.Identity;
    private bool _localDirty = true;
    private bool _worldDirty = true;

    public Matrix4 LocalMatrix
    {
        get
        {
            if (_localDirty)
            {
                _localMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
                _localDirty = false;
                _worldDirty = true;
            }
            return _localMatrix;
        }
    }

    public Matrix4 WorldMatrix
    {
        get
        {
            if (_worldDirty)
            {
                _worldMatrix = Parent != null ? LocalMatrix * Parent.WorldMatrix : LocalMatrix;
                _worldDirty = false;
            }
            return _worldMatrix;
        }
    }

    public void AddChild(SceneNode child)
    {
        if (child == null) return;
        if (child == this) return;
        if (child.Parent == this) return;
        child.Parent = this;
    }

    public void RemoveChild(SceneNode child)
    {
        if (child == null) return;
        if (child.Parent != this) return;
        child.Parent = null;
    }

    public void MarkDirty()
    {
        _localDirty = true;
        _worldDirty = true;
        foreach (var child in Children)
            child.MarkDirty();
    }

    public void Move(Vector3 worldTranslation)
    {
        Vector3 worldPos = new Vector3(WorldMatrix.M41, WorldMatrix.M42, WorldMatrix.M43);
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
        MarkDirty();
    }

    public void Translate(Vector3 localTranslation)
    {
        Position += Rotation * localTranslation;
        MarkDirty();
    }

    public Quaternion GetWorldRotation()
    {
        if (Parent != null)
            return Parent.GetWorldRotation() * Rotation;
        return Rotation;
    }


    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Normalize(Vector3.Cross(forward, right));
        Matrix3 m = new Matrix3(right, up, forward);
        return Quaternion.FromMatrix(m);
    }



    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);
        float cosTheta = Vector3.Dot(from, to);
        if (cosTheta >= 1.0f - 1e-6f)
            return Quaternion.Identity;
        if (cosTheta < -1.0f + 1e-6f)
        {
            Vector3 rotationAxis = Vector3.Cross(from, Vector3.UnitY);
            if (rotationAxis.LengthSquared < 1e-6f)
                rotationAxis = Vector3.Cross(from, Vector3.UnitX);
            rotationAxis = Vector3.Normalize(rotationAxis);
            return Quaternion.FromAxisAngle(rotationAxis, MathHelper.Pi);
        }
        Vector3 rotationAxis2 = Vector3.Cross(from, to);
        float s = MathF.Sqrt((1 + cosTheta) * 2);
        float invs = 1 / s;
        return new Quaternion(rotationAxis2 * invs, s * 0.5f);
    }


    public void Rotate(Quaternion delta)
    {
        Rotation = delta * Rotation;
        MarkDirty();
    }

    public void Rotate(Vector3 eulerAngles)
    {
        Vector3 radians = new Vector3(
            MathHelper.DegreesToRadians(eulerAngles.X),
            MathHelper.DegreesToRadians(eulerAngles.Y),
            MathHelper.DegreesToRadians(eulerAngles.Z));
        Quaternion delta = Quaternion.FromEulerAngles(radians);
        Rotate(delta);
    }

    public Vector3 GetWorldPosition()
    {
        return new Vector3(WorldMatrix.M41, WorldMatrix.M42, WorldMatrix.M43);
    }

    public void SetRotation(Quaternion rot)
    {
        Rotation = rot;
        MarkDirty();
    }

    public void SetRotation(Vector3 eulerAngles)
    {
        Vector3 radians = new Vector3(
            MathHelper.DegreesToRadians(eulerAngles.X),
            MathHelper.DegreesToRadians(eulerAngles.Y),
            MathHelper.DegreesToRadians(eulerAngles.Z));
        Rotation = Quaternion.FromEulerAngles(radians);
        MarkDirty();
    }

    public void ScaleBy(Vector3 scaleFactor)
    {
        if (!Resizable) return;
        Scale *= scaleFactor;
        MarkDirty();
    }

    public void ScaleBy(float s)
    {
        ScaleBy(new Vector3(s));
    }

    public void SetPosition(Vector3 pos)
    {
        Position = pos;
        MarkDirty();
    }

    public void SetScale(Vector3 scale)
    {
        if (!Resizable) return;
        Scale = scale;
        MarkDirty();
    }
    public void SetScale(float scale)
    {
        SetScale(new Vector3(scale, scale, scale));
    }

    public void Update()
    {
        var _ = WorldMatrix;
        foreach (var child in Children)
            child.Update();
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

    public void RotateAround(Vector3 point, Vector3 axis, float angleDegrees)
    {
        float angleRadians = MathHelper.DegreesToRadians(angleDegrees);
        Matrix4 rotationMatrix = Matrix4.CreateFromAxisAngle(axis.Normalized(), angleRadians);
        Vector3 direction = Position - point;
        direction = Vector3.TransformNormal(direction, rotationMatrix);
        Position = point + direction;
        Quaternion rot = Quaternion.FromAxisAngle(axis.Normalized(), angleRadians);
        Rotation = rot * Rotation;
        MarkDirty();
    }

    public void ResetTransform()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
        MarkDirty();
    }
    public SceneNode? FindChildByName(string name)
    {
        return FindChild(node => node.Name == name);
    }

    public List<SceneNode> FindAllChildrenByName(string name)
    {
        List<SceneNode> found = new List<SceneNode>();
        Traverse(node => { if (node.Name == name) found.Add(node); });
        return found;
    }

    public List<SceneNode> FindAllChildrenByTag(string tag)
    {
        List<SceneNode> found = new List<SceneNode>();
        Traverse(node => { if (node.Tag == tag) found.Add(node); });
        return found;
    }

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


    public void UpdateAll()
    {
        if (this is IGameComponent component)
            component.Update();
        foreach (var child in Children)
        {
            if (child is SceneNode node)
                node.Update();
        }
    }
    public void RenderAll(Camera camera)
    {
        if (this is IGameComponent component)
            component.Render();
        foreach (var child in Children)
        {
            if (child is SceneNode node)
                node.RenderAll(camera);
        }
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SceneNode);
    }
    public bool Equals(SceneNode other)
    {
        if (ReferenceEquals(other, null))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    public static bool operator ==(SceneNode left, SceneNode right)
    {
        if (ReferenceEquals(left, null))
            return ReferenceEquals(right, null);

        return left.Equals(right);
    }


    public static bool operator !=(SceneNode left, SceneNode right)
    {
        return !(left == right);
    }

}
