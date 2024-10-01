using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Transform :  IEquatable<Transform>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "Tranform";

        public virtual Vector3 Position { get; set; } = Vector3.Zero;


        public Vector3 Rotation { get; set; } = Vector3.Zero; // Euler angles in degrees


        public virtual Vector3 Scale { get; set; } = Vector3.One;


        public Matrix4 GetModelMatrix()
        {
            var translation = Matrix4.CreateTranslation(Position);
            var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            var rotation = rotationZ * rotationY * rotationX;
            var scale = Matrix4.CreateScale(Scale);
            return scale * rotation * translation;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Transform);
        }

        public bool Equals(Transform other)
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

     
        public static bool operator ==(Transform left, Transform right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

       
        public static bool operator !=(Transform left, Transform right)
        {
            return !(left == right);
        }

    }
}
