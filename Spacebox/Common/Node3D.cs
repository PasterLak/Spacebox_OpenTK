using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Node3D :  IEquatable<Node3D>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "Tranform";

        public virtual Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero; // Euler
        public virtual Vector3 Scale { get; set; } = Vector3.One;

        public bool Resizable { get; protected set; } = true;

        public Node3D Parent { get;  set; } = null;
        public bool HasParent => Parent != null;

        public List<Node3D> Children { get; protected set; } = new List<Node3D>();
        public bool HasChildren => Children.Count > 0;

        public virtual void AddChild(Node3D node)
        {
            if (Children.Contains(node)) return;
           
            Children.Add(node);
            node.Parent = this;
            
        }

        public void Rotate(float x,float y,float z)
        {
            Rotation += new Vector3(x,y,z);
        }


        public Matrix4 GetModelMatrix()
        {

            bool relativeToCamera = false;

            if (Camera.Main != null && Camera.Main.CameraRelativeRender) relativeToCamera = true;

            var pos = Position;

            if(relativeToCamera )
            {
                if(Parent == null)
                pos = Position - Camera.Main.Position;
                else
                    pos = Position;

            }


            Matrix4 localTransform = GetModelMatrixPoor(pos); 

            if (Parent != null)
            {
                return localTransform * Parent.GetModelMatrix() ; 
            }
            else
            {
                return localTransform;
            }
        }


        public Matrix4 GetModelMatrixPoor()
        {
            return GetModelMatrixPoor(Position);
        }
        private Matrix4 GetModelMatrixPoor(Vector3 pos)
        {
            var translation = Matrix4.CreateTranslation(pos);
            var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            var rotation = rotationZ * rotationY * rotationX;
            var scale = Resizable ? Matrix4.CreateScale(Scale) : Matrix4.Identity;

            return  scale * rotation * translation;
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
