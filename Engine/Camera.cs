using OpenTK.Mathematics;
using Engine.Physics;

namespace Engine
{
    public abstract class Camera : DynamicBody
    {
        public static Camera Main;
        protected Vector3 _front = -Vector3.UnitZ;
        protected Vector3 _up = Vector3.UnitY;
        protected Vector3 _right = Vector3.UnitX;

        private float _fov = MathHelper.DegreesToRadians(80f);
        public float AspectRatio { get; set; } = 16f / 9f;
        public float DepthNear = 0.1f;
        public float DepthFar = 2000f;
        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        public bool CameraRelativeRender = false;

        public CameraFrustum Frustum { get; private set; } = new CameraFrustum();

        public float FOV
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 180f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        protected Camera(Vector3 position, bool isMainCamera = true)
            : base(new BoundingSphere(position, 0.5f))
        {
            if (isMainCamera)
                Main = this;
            Position = position;

            AllowCollisionDebug = false;

            UpdateVectors();

        }

        public virtual void Update() { }


        public virtual Matrix4 GetViewMatrix()
        {
            if (CameraRelativeRender)
            {
                return Matrix4.LookAt(Vector3.Zero, _front, _up);

            }
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, DepthNear, DepthFar);
        }

        protected abstract void UpdateVectors();

        public Vector2 WorldToScreenPoint(Vector3 worldPosition, int screenWidth, int screenHeight)
        {
            if (CameraRelativeRender) worldPosition = worldPosition - Position;

            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();

            Vector4 worldPos = new Vector4(worldPosition, 1.0f);

            Vector4 viewPos = worldPos * viewMatrix;
            Vector4 projPos = viewPos * projectionMatrix;

            if (Math.Abs(projPos.W) < 1e-6f)
                return Vector2.Zero;


            Vector3 ndc = new Vector3(
                projPos.X / projPos.W,
                projPos.Y / projPos.W,
                projPos.Z / projPos.W
            );

            if (ndc.X < -1.0f || ndc.X > 1.0f ||
                ndc.Y < -1.0f || ndc.Y > 1.0f ||
                ndc.Z < -1.0f || ndc.Z > 1.0f)
                return Vector2.Zero;

            Vector2 screenPos = new Vector2(
                (ndc.X + 1.0f) * 0.5f * screenWidth,
                (1.0f - ndc.Y) * 0.5f * screenHeight
            );

            return screenPos;
        }
    }
}
