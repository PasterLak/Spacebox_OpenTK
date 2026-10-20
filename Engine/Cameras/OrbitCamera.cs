using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine
{
    public class OrbitalCamera : Camera
    {
        public bool CanMove = true;
        public MouseButton RotateButton = MouseButton.Button1;
        public MouseButton PanButton = MouseButton.Middle;
        private Vector3 _target;
        private float _distance;
        private readonly float _minDistance;
        private readonly float _maxDistance;
        private float _yaw;
        private float _pitch;
        private readonly float _rotationSpeed;
        private readonly float _zoomSpeed;
        private readonly float _panSpeed;
        private bool _rotating;
        private bool _panning;
        private Vector2 _lastMousePos;

        public OrbitalCamera(Vector3 target, float initialDistance = 10f, float minDistance = 1f, float maxDistance = 100f, bool isMain = true)
            : base(target - new Vector3(0, 0, initialDistance), isMain)
        {
            _target = target;
            _distance = initialDistance;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            _rotationSpeed = 0.2f;
            _zoomSpeed = 1f;
            _panSpeed = 0.005f;
            _yaw = -90f;
            _pitch = 0f;
            UpdateVectors();
        }

        public override void Update()
        {
            if (!CanMove) return;

           
            var pos = Input.Mouse.Position;

            if (Input.IsMouseButton(RotateButton))
            {
                if (!_rotating)
                {
                    _rotating = true;
                    _lastMousePos = pos;
                }
                var d = pos - _lastMousePos;
                _yaw -= d.X * _rotationSpeed;
                _pitch += d.Y * _rotationSpeed;
                _lastMousePos = pos;
            }
            else
            {
                _rotating = false;
            }

            if (Input.IsMouseButton(PanButton))
            {
                if (!_panning)
                {
                    _panning = true;
                    _lastMousePos = pos;
                }
                var d = pos - _lastMousePos;
                _target += (_right * -d.X + _up * d.Y) * _panSpeed;
                _lastMousePos = pos;
            }
            else
            {
                _panning = false;
            }

            _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

            var scroll = Input.Mouse.ScrollDelta.Y;
            if (scroll != 0f)
                HandleZoom(scroll);

            Position = _target + CalculateOffset();
            UpdateVectors();
        }

        private void HandleZoom(float scroll)
        {
            if (Projection == ProjectionType.Orthographic)
            {
                OrthoSize = MathHelper.Clamp(
                    OrthoSize - scroll * _zoomSpeed,
                    _minDistance,
                    _maxDistance
                );
            }
            else
            {
                _distance = MathHelper.Clamp(
                    _distance - scroll * _zoomSpeed,
                    _minDistance,
                    _maxDistance
                );
            }
        }

        private Vector3 CalculateOffset()
        {
            var yRad = MathHelper.DegreesToRadians(_yaw);
            var pRad = MathHelper.DegreesToRadians(_pitch);
            return new Vector3(
                _distance * MathF.Cos(pRad) * MathF.Cos(yRad),
                _distance * MathF.Sin(pRad),
                _distance * MathF.Cos(pRad) * MathF.Sin(yRad)
            );
        }

        protected override void UpdateVectors()
        {
            _front = (_target - Position).Normalized();
            _right = Vector3.Cross(_front, Vector3.UnitY).Normalized();
            _up = Vector3.Cross(_right, _front).Normalized();
        }

        public void SetTarget(Vector3 newTarget)
        {
            _target = newTarget;
        }
    }
}
