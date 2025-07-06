using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine.Physics;
using Spacebox.GUI;

namespace Spacebox.Game.Player
{
    public class FreeCamera0 : Camera360Base
    {
        private float _cameraSpeed = 2.5f;
        private float _shiftSpeed = 5.5f;
        private float _sensitivity = 0.0025f;
        private bool _firstMouseMove = true;
        private Vector2 _lastMousePosition;
        private Quaternion _orientation = Quaternion.Identity;
        public bool CameraActive = true;

        public FreeCamera0(Vector3 position, bool isMainCamera = true)
            : base(position, isMainCamera)
        {
            _orientation = Quaternion.Identity;
            UpdateVectors();
        }

        protected override void UpdateVectors()
        {
            _front = Vector3.Transform(-Vector3.UnitZ, _orientation);
            _up = Vector3.Transform(Vector3.UnitY, _orientation);
            _right = Vector3.Normalize(Vector3.Cross(_front, _up));
        }

        public override void Update()
        {
            HandleInput();
            base.Update();
        }

        private void HandleInput()
        {
            var mouse = Input.Mouse;
            float currentSpeed = Input.IsKey(Keys.LeftShift) ? _shiftSpeed : _cameraSpeed;
            Vector3 movement = Vector3.Zero;
            movement += Vector3.Transform(-Vector3.UnitZ, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.W) ? 1 : 0);
            movement -= Vector3.Transform(-Vector3.UnitZ, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.S) ? 1 : 0);
            movement -= Vector3.Transform(Vector3.UnitX, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.A) ? 1 : 0);
            movement += Vector3.Transform(Vector3.UnitX, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.D) ? 1 : 0);
            movement += Vector3.Transform(Vector3.UnitY, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.Q) ? 1 : 0);
            movement -= Vector3.Transform(Vector3.UnitY, _orientation) * currentSpeed * (float)Time.Delta * (Input.IsKey(Keys.E) ? 1 : 0);
            Position += movement;

            if (_firstMouseMove)
            {
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                _firstMouseMove = false;
            }
            else if (CameraActive)
            {
                float deltaX = mouse.X - _lastMousePosition.X;
                float deltaY = mouse.Y - _lastMousePosition.Y;
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);

                Vector3 localUp = Vector3.Transform(Vector3.UnitY, _orientation);
                Vector3 localRight = Vector3.Transform(Vector3.UnitX, _orientation);

                Quaternion qYaw = Quaternion.FromAxisAngle(localUp, -deltaX * _sensitivity);
                Quaternion qPitch = Quaternion.FromAxisAngle(localRight, -deltaY * _sensitivity);
                _orientation = qYaw * qPitch * _orientation;
                _orientation = Quaternion.Normalize(_orientation);
                UpdateVectors();
            }
        }

        public override Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }
    }
}
