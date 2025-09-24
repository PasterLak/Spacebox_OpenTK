using OpenTK.Mathematics;
using Engine.Physics;
using Engine.Components;

namespace Engine
{
    public enum ProjectionType : byte
    {
        Perspective,
        Orthographic
    }
    public abstract class Camera : DynamicBody
    {
        private static Camera _instance;
        public static Camera Main
        {
            get { return _instance; }
            set
            {
                var oldCam = _instance;
                _instance = value;

                if(_instance != null)
                _instance.OnMainCameraChanged();

                if( oldCam != null )
                oldCam.OnMainCameraChanged();
            }
        }

        private ProjectionType _projectionType = ProjectionType.Perspective;
        public ProjectionType Projection
        {
            get => _projectionType;
            set => _projectionType = value;
        }
        private float _orthoSize = 10f;
        public float OrthoSize
        {
            get => _orthoSize;
            set => _orthoSize = MathHelper.Max(0.1f, value);
        }

        protected Vector3 _front = -Vector3.UnitZ;
        protected Vector3 _up = Vector3.UnitY;
        protected Vector3 _right = Vector3.UnitX;

        private float _fov = MathHelper.DegreesToRadians(80f);
        public float AspectRatio { get; set; } = 16f / 9f;
        public float DepthNear = 0.1f;
        public float DepthFar = 800f;
        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        public void OnMainCameraChanged()
        {
            if(gizmo != null)
            {
                
                gizmo.Enabled = this != Main;
            }
           
        }
        public bool IsMain => Main == this;
        private bool _cameraRelativeRender = false;

        public bool CameraRelativeRender => _cameraRelativeRender;
        public void SetRenderSpace(bool renderSpace)
        {
            _cameraRelativeRender = renderSpace;
        }

        public CameraFrustum Frustum { get; private set; } = new CameraFrustum();
        private Component gizmo;
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
            Name = "Camera";
            UpdateVectors();


           
            //var gizmoCam = Resources.Load<Texture2D>("Resources/Textures/Gizmos/camera.png");
           // gizmo = AttachComponent(new GizmoIconComponent(gizmoCam, 1f,1f));

        }

        public override void Update()
        {
            if(IsMain)
            {
                RenderSpace.UpdateOrigin();
                Frustum.UpdateFrustum(this);
            }
               
            base.Update();
        }


        public virtual Matrix4 GetViewMatrix()
        {
            if (_cameraRelativeRender)
            {
                return Matrix4.LookAt(Vector3.Zero, _front, _up);

            }
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            switch (_projectionType)
            {
                case ProjectionType.Orthographic:
                    float halfHeight = _orthoSize;
                    float halfWidth = _orthoSize * AspectRatio;
                    return Matrix4.CreateOrthographicOffCenter(
                        -halfWidth, halfWidth,
                        -halfHeight, halfHeight,
                         DepthNear, DepthFar
                    );
                case ProjectionType.Perspective:
                default:
                    return Matrix4.CreatePerspectiveFieldOfView(
                        _fov, AspectRatio, DepthNear, DepthFar
                    );
            }
        }
        protected abstract void UpdateVectors();


        public Vector2 WorldToScreenPoint(Vector3 worldPosition, int screenWidth, int screenHeight)
        {
            worldPosition = RenderSpace.ToRender(worldPosition);

            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();

            Vector4 clip = new Vector4(worldPosition, 1.0f) * viewMatrix * projectionMatrix;

            if (clip.W <= 0.000001f)
                return Vector2.Zero;

            Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;

            if (ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f)
                return Vector2.Zero;

            return new Vector2(
                (ndc.X + 1f) * 0.5f * screenWidth,
                (1f - ndc.Y) * 0.5f * screenHeight
            );
        }

    }
}
