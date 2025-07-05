using OpenTK.Mathematics;

namespace Engine
{
    public class Node3D :  IEquatable<Node3D>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "Transform";

        public virtual Vector3 Position
        {
            get => position;
            set { position = value; MarkDirty(); }
        }
        Vector3 position = Vector3.Zero;
        public Vector3 Rotation
        {
            get => rotation;
            set { rotation = value; MarkDirty(); }
        }
        Vector3 rotation = Vector3.Zero; // Euler
        public virtual Vector3 Scale
        {
            get => scale;
            set { scale = value; MarkDirty(); }
        }
        Vector3 scale = Vector3.One;

        public bool Resizable { get; protected set; } = true;

        public Node3D Parent { get;  set; } = null;
        public bool HasParent => Parent != null;

        public List<Node3D> Children { get; protected set; } = new List<Node3D>();
        public bool HasChildren => Children.Count > 0;

        private bool dirty = true;
        private Matrix4 cachedModelMatrix;

        public virtual void AddChild(Node3D node)
        {
            if (Children.Contains(node)) return;
           
            Children.Add(node);
            node.Parent = this; 
        }
        public static Vector3 QuaternionToEulerDegrees(Quaternion q)
        {

            Vector3 eulerRad = q.ToEulerAngles();

            float xDegrees = eulerRad.X * (180f / (float)Math.PI);
            float yDegrees = eulerRad.Y * (180f / (float)Math.PI);
            float zDegrees = eulerRad.Z * (180f / (float)Math.PI);

            return new Vector3(xDegrees, yDegrees, zDegrees);
        }
        private void MarkDirty()
        {
            if (!dirty)
            {
                dirty = true;
                for (int i = 0; i < Children.Count; i++)
                    Children[i].MarkDirty();
            }
        }

        public void Rotate(Vector3 rot)
        {
            Rotate(rot.X, rot.Y, rot.Z);
        }

        public void Rotate(float x,float y,float z)
        {
            Rotation += new Vector3(x,y,z);
        }

        private bool relative = false;
        public Matrix4 GetModelMatrix()
        {

            bool relativeToCamera = (Camera.Main != null && Camera.Main.CameraRelativeRender);

            if(relative != relativeToCamera)
            {
                relative = relativeToCamera;
                dirty = true;
            }

            var pos = Position;

            if(relativeToCamera )
            {
                if(Parent == null)
                pos = Position - Camera.Main.Position;

                dirty = true;
            }

            if (Parent != null)
            {
                return GetModelMatrixPoor(pos) * Parent.GetModelMatrix(); 
            }
            else
            {
                return GetModelMatrixPoor(pos);
            }
        }


        public Matrix4 GetModelMatrixPoor()
        {
            return GetModelMatrixPoor(Position);
        }
        private Matrix4 GetModelMatrixPoor(Vector3 pos)
        {
            if(!dirty)
            {
                return cachedModelMatrix;
            }

            var translation = Matrix4.CreateTranslation(pos);
            var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            var rotation = rotationZ * rotationY * rotationX;
            var scale = Resizable ? Matrix4.CreateScale(Scale) : Matrix4.Identity;

            dirty = false;
            cachedModelMatrix = scale * rotation * translation;
            return cachedModelMatrix;
        }

        public Vector3 GetWorldPosition()
        {
            if (Parent != null)
            {
                if(Rotation == Vector3.Zero && Parent.Rotation == Vector3.Zero)
                {
                    return Parent.GetWorldPosition() + Position;
                }

                return LocalToWorld(Position, Parent);
            }
            else
            {
                return Position;
            }
        }

        public static Vector3 WorldToLocal(Vector3 worldPosition, Node3D node)
        {
            Matrix4 invModel = node.GetModelMatrixPoor().Inverted();
            return Vector3.TransformPosition(worldPosition, invModel);
        }

        public static Vector3 LocalToWorld(Vector3 localPosition, Node3D node)
        {
            return Vector3.TransformPosition(localPosition, node.GetModelMatrixPoor());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Node3D);
        }

        public bool Equals(Node3D other)
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
     
        public static Vector3 QuaternionToEuler(Quaternion q)
        {
         
            q = Quaternion.Normalize(q);

        
            float sinr_cosp = 2f * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            float x = MathF.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2f * (q.W * q.Y - q.Z * q.X);
            float y;
            if (MathF.Abs(sinp) >= 1f)
                y = MathF.CopySign(MathF.PI / 2f, sinp); 
            else
                y = MathF.Asin(sinp);

            float siny_cosp = 2f * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            float z = MathF.Atan2(siny_cosp, cosy_cosp);

            return new Vector3(x, y, z);
        }


        public static bool operator ==(Node3D left, Node3D right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

       
        public static bool operator !=(Node3D left, Node3D right)
        {
            return !(left == right);
        }

    }
}
