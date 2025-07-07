using Engine.Components;
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
        public List<Node3D> Children { get; } = new();
        public bool HasChildren => Children.Count > 0;
        public List<Component> Components { get; } = new();
        public bool HasComponents => Components.Count > 0;
        
        public bool Enabled { get; set; } = true;   
        
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
            child.Parent = this;
            return child;
        }

        //  ──────────────────────────────────────────────────────────────────────────────
        //  Вставь в Node3D (или в статический helper-класс, если удобней).
        //  ──────────────────────────────────────────────────────────────────────────────
        public T AddChildKeepWorld<T>(T child) where T : Node3D
        {
            if (child == null || ReferenceEquals(child, this)) return child;

            // 1. запоминаем текущую мировую матрицу ребёнка
            Matrix4 worldChild = child.GetModelMatrix();

            // 2. назначаем нового родителя (без перестройки трансформа)
            child._parent?.Children.Remove(child);
            child._parent = this;
            Children.Add(child);

            // 3. вычисляем локальный = worldChild * inverse(parentWorld)
            Matrix4 local = worldChild * Matrix4.Invert(GetModelMatrix());

            // 4. декомпозируем и пишем как локальный трансформ
            child.Position = local.ExtractTranslation();
            child.Scale = local.ExtractScale();
            child.Rotation = QuaternionToEulerDegrees(local.ExtractRotation());

            child.MarkDirty();
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

        public virtual void Update()
        {
            if(!Enabled) return;
            
            _ = GetModelMatrix();
            for (int i = 0; i < Components.Count; i++)
                if (Components[i].Enabled) Components[i].OnUpdate();
            for (int i = 0; i < Children.Count; i++)
                Children[i].Update();
        }

        public virtual void Render()
        {
            if(!Enabled) return;
            
            for (var i = 0; i < Components.Count; i++)
            {
                var cmp = Components[i];
                if (cmp.Enabled)
                {
                    cmp.OnRender();
                }
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
            return Vector3.TransformPosition(localPosition, node.GetModelMatrixPoor());
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

        public Vector3 GetWorldPosition() => Parent == null ? Position : Parent.LocalToWorld(Position);

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

        public Vector3 GetWorldPositionRaw()
        {
            return Parent == null
                ? Position
                : Vector3.TransformPosition(Position, Parent.GetModelMatrixPoor());
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

        public Vector3 Forward
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

        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));


        protected void PrintHierarchy()
        {
            Debug.Log("------------------- SceneGraph -------------------");
            PrintHierarchy(0);
            Debug.Log("------------------- SceneGraph -------------------");
        }

        private void PrintHierarchy(int depth = 0)
        {
            Debug.Log($"{new string(' ', depth * 2)}{Name} [{Components.Count}]");
            foreach (var child in Children)
                child.PrintHierarchy(depth + 1);
        }

    }
}
