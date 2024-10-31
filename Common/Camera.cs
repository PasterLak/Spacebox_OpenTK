
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public abstract class Camera : DynamicBody
    {
        protected Vector3 _front = -Vector3.UnitZ;
        protected Vector3 _up = Vector3.UnitY;
        protected Vector3 _right = Vector3.UnitX;

        public float FOV = MathHelper.DegreesToRadians(80f);

        public float AspectRatio { get; set; }

        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(FOV);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                FOV = MathHelper.DegreesToRadians(angle);
            }
        }

        protected Camera(Vector3 position, float aspectRatio)
            : base(new BoundingSphere(position, 0.5f))
        {
            Position = position;
            AspectRatio = aspectRatio;
            UpdateVectors();
        }

        public virtual void Update()
        {
            if (Debug.ShowDebug)
            {
                Debug.ProjectionMatrix = GetProjectionMatrix();
                Debug.ViewMatrix = GetViewMatrix();
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(FOV, AspectRatio, 0.01f, 1000f);
        }

        protected abstract void UpdateVectors();
    }
}
