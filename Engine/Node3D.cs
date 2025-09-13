using Engine.Components;
using Engine.Light;
using OpenTK.Mathematics;

namespace Engine
{
    public class Node3D : Transform3D, IEquatable<Node3D>
    {
        private readonly Guid _id = Guid.NewGuid();
        private Node3D? _parent;

        public Guid Id => _id;
        public string Name { get; set; } = "Node3D";
        public string Tag { get; set; } = "Default";
        public bool Resizable { get; protected set; } = true;

        public Node3D? Parent
        {
            get => _parent;
            set
            {
                if (_parent == value) return;
                _parent?.Children.Remove(this);
                _parent = value;
                _parent?.Children.Add(this);
                MarkDirty();
            }
        }

        public bool HasParent => _parent != null;
        public bool IsRoot => !HasParent;
        public List<Node3D> Children { get; } = new();
        public bool HasChildren => Children.Count > 0;
        public List<Component> Components { get; } = new();
        public bool HasComponents => Components.Count > 0;

        private bool _enabled = true;
        public bool Enabled { get => _enabled; set { _enabled = value; OnEnabledChanged?.Invoke(_enabled); } }
        public Action<bool> OnEnabledChanged { get; set; }

        public Node3D()
        {
            Owner = this;

        }
        public Node3D(Vector3 position)
        {
            Position = position;
            Owner = this;
        }

        public T AddChild<T>(T child) where T : Node3D
        {
            if (child == null || ReferenceEquals(child, this)) return child;
            if (child is Skybox)
            {
                Lighting.Skybox = child;
            }
            child.Parent = this;
            return child;
        }


        public void RemoveChild(Node3D child)
        {
            if (child == null) return;
            if (Children.Remove(child))
            {
                child._parent = null;
                child.MarkDirty();
            }
        }

        public T AttachComponent<T>(T component) where T : Component
        {
            if (component == null || Components.Contains(component)) return component;

            component.OnAttached(this);
            Components.Add(component);
            return component;
        }

        public void DetachComponent(Component component)
        {
            if (component == null) return;
            if (Components.Remove(component)) component.OnDetached();
        }

        public virtual void Start()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i].Enabled)
                    Components[i].Start();
            }

            for (int i = Children.Count - 1; i >= 0; i--)
            {
                Children[i].Start();
            }
        }
        public virtual void Destroy()
        {

            for (int i = 0; i < Components.Count; i++)
            {
                DetachComponent(Components[i]);
            }

            for (int i = Children.Count - 1; i >= 0; i--)
            {
                var child = Children[i];
                RemoveChild(child);
                child.Destroy();
            }


            Parent?.RemoveChild(this);
        }
        public virtual void Update()
        {
            if (!Enabled) return;

            _ = GetModelMatrix();
            for (int i = 0; i < Components.Count; i++)
                if (Components[i].Enabled) Components[i].OnUpdate();
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Enabled)
                    Children[i].Update();
            }

        }

        public virtual void Render()
        {
            if (!Enabled) return;

            for (var i = 0; i < Components.Count; i++)
            {
                var cmp = Components[i];
                if (cmp.Enabled)
                {
                    cmp.OnRender();
                }
            }
            for (int i = 0; i < Children.Count; i++)
            {
               // if (Children[i].Enabled)
               //     Children[i].Render();
            }
        }

        public void Traverse(Action<Node3D>? action)
        {
            if (action == null) return;
            action(this);
            for (int i = 0; i < Children.Count; i++)
                Children[i].Traverse(action);
        }

        public Node3D? FindChild(Predicate<Node3D> predicate)
        {
            if (predicate == null) return null;
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (predicate(child)) return child;
                var found = child.FindChild(predicate);
                if (found != null) return found;
            }
            return null;
        }

        public Node3D? FindChildByName(string name) => FindChild(n => n.Name == name);

        public List<Node3D> FindAllChildrenByName(string name)
        {
            var list = new List<Node3D>();
            Traverse(n => { if (n.Name == name) list.Add(n); });
            return list;
        }

        public List<Node3D> FindAllChildrenByTag(string tag)
        {
            var list = new List<Node3D>();
            Traverse(n => { if (n.Tag == tag) list.Add(n); });
            return list;
        }

        public Node3D GetRoot()
        {
            if (HasParent)
            {
                return Parent.GetRoot();
            }
            else
                return this;
        }

        public void Rotate(Vector3 rot) => Rotate(rot.X, rot.Y, rot.Z);

        public void Rotate(float x, float y, float z)
        {
            Rotation += new Vector3(x, y, z);
        }

        public void SetScale(float scale) => Scale = new Vector3(scale);

        public void ScaleBy(Vector3 factor)
        {
            Scale *= factor;
            MarkDirty();
        }

        public void ScaleBy(float factor) => ScaleBy(new Vector3(factor));

        public Vector3 WorldToLocal(Vector3 worldPoint)
        {
            var inv = Matrix4.Invert(GetModelMatrix());
            return Vector3.TransformPosition(worldPoint, inv);
        }

        public Vector3 LocalToWorld(Vector3 localPoint) => Vector3.TransformPosition(localPoint, GetModelMatrix());

        public static Vector3 LocalToWorld(Vector3 localPosition, Node3D node)
        {
            return Vector3.TransformPosition(localPosition, node.GetModelMatrix());
        }
        public void Translate(Vector3 localTranslation)
        {
            var q = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(Rotation.X),
                MathHelper.DegreesToRadians(Rotation.Y),
                MathHelper.DegreesToRadians(Rotation.Z));
            Position += Vector3.Transform(localTranslation, q);
        }

        public void RotateAround(Vector3 point, Vector3 axis, float angleDegrees)
        {
            var radians = MathHelper.DegreesToRadians(angleDegrees);
            var rotationMatrix = Matrix4.CreateFromAxisAngle(axis.Normalized(), radians);
            var direction = Position - point;
            direction = Vector3.TransformNormal(direction, rotationMatrix);
            Position = point + direction;
            var rot = Quaternion.FromAxisAngle(axis.Normalized(), radians);
            Rotation = QuaternionToEulerDegrees(rot * ToQuaternion(Rotation));
        }

        public Vector3 PositionWorld => Parent == null ? Position : Parent.LocalToWorld(Position);

        public bool Equals(Node3D? other) => !(other is null) && _id.Equals(other._id);

        public override bool Equals(object? obj) => Equals(obj as Node3D);

        public override int GetHashCode() => _id.GetHashCode();

        public static bool operator ==(Node3D? left, Node3D? right) => Equals(left, right);

        public static bool operator !=(Node3D? left, Node3D? right) => !Equals(left, right);

        public static Vector3 QuaternionToEulerDegrees(Quaternion q)
        {
            var rad = QuaternionToEuler(q);
            return new Vector3(
                MathHelper.RadiansToDegrees(rad.X),
                MathHelper.RadiansToDegrees(rad.Y),
                MathHelper.RadiansToDegrees(rad.Z));
        }


        public static Vector3 QuaternionToEuler(Quaternion q)
        {
            q = Quaternion.Normalize(q);
            var sinr_cosp = 2f * (q.W * q.X + q.Y * q.Z);
            var cosr_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            var x = MathF.Atan2(sinr_cosp, cosr_cosp);

            var sinp = 2f * (q.W * q.Y - q.Z * q.X);
            var y = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

            var siny_cosp = 2f * (q.W * q.Z + q.X * q.Y);
            var cosy_cosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            var z = MathF.Atan2(siny_cosp, cosy_cosp);

            return new Vector3(x, y, z);
        }

        private static Quaternion ToQuaternion(Vector3 eulerDegrees)
        {
            var rad = new Vector3(
                MathHelper.DegreesToRadians(eulerDegrees.X),
                MathHelper.DegreesToRadians(eulerDegrees.Y),
                MathHelper.DegreesToRadians(eulerDegrees.Z));
            return Quaternion.FromEulerAngles(rad);
        }


        public Vector3 ForwardWorld
        {
            get
            {

                Matrix4 m = GetModelMatrix();

                Vector3 dir = Vector3.Transform(new Vector3(0, 0, -1), m.ExtractRotation()).Normalized();

                return dir;
            }
        }
        public void RotateByNormal(Vector3 normal)
        {
            if (normal.LengthSquared == 0f) return;

            normal.Normalize();

            float yaw = MathF.Atan2(normal.X, normal.Z);
            float pitch = MathF.Atan2(-normal.Y, MathF.Sqrt(normal.X * normal.X + normal.Z * normal.Z));

            Rotation = new Vector3(
                MathHelper.RadiansToDegrees(pitch),
                MathHelper.RadiansToDegrees(yaw),
                0f);
        }

        public Vector3 ForwardLocal
        {
            get
            {
                var q = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(Rotation.X),
                    MathHelper.DegreesToRadians(Rotation.Y),
                    MathHelper.DegreesToRadians(Rotation.Z));
                return Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, q));
            }
        }

        public Vector3 Right => Vector3.Normalize(Vector3.Cross(ForwardLocal, Vector3.UnitY));
        public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, ForwardLocal));


        public T FindNode<T>() where T : Node3D
        {
            if (this is T result) return result;

            for (int i = 0; i < Children.Count; i++)
            {
                var found = Children[i].FindNode<T>();
                if (found != null) return found;
            }

            return null;
        }

        public List<T> FindAllNodesOfType<T>() where T : Node3D
        {
            var results = new List<T>();
            FindAllNodesOfTypeRecursive(results);
            return results;
        }

        private void FindAllNodesOfTypeRecursive<T>(List<T> results) where T : Node3D
        {
            if (this is T match) results.Add(match);

            for (int i = 0; i < Children.Count; i++)
                Children[i].FindAllNodesOfTypeRecursive(results);
        }

        public T FindComponent<T>() where T : Component
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T component) return component;
            }

            for (int i = 0; i < Children.Count; i++)
            {
                var found = Children[i].FindComponent<T>();
                if (found != null) return found;
            }

            return null;
        }

        public List<T> FindAllComponents<T>() where T : Component
        {
            var results = new List<T>();
            FindAllComponentsRecursive(results);
            return results;
        }

        private void FindAllComponentsRecursive<T>(List<T> results) where T : Component
        {
            for (int i = 0; i < Components.Count; i++)
            {
                if (Components[i] is T component) results.Add(component);
            }

            for (int i = 0; i < Children.Count; i++)
                Children[i].FindAllComponentsRecursive(results);
        }


        protected void PrintHierarchy()
        {
            Debug.Log("------------------- SceneGraph -------------------");
            var objectsCount = PrintHierarchy(0);
            Debug.Log($"------------------- SceneGraph [{objectsCount}] -----------------");
        }

        private int PrintHierarchy(int depth = 0, int count = 1)
        {
            Debug.Log($"[{Components.Count}]{new string('-', depth * 2)}{Name}");

            foreach (var child in Children)
                count = child.PrintHierarchy(depth + 1, ++count);

            return count;
        }

    }
}
