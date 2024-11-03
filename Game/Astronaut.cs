using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using System;

namespace Spacebox.Game
{
    public class Astronaut : Camera360, INotTransparent
    {
        private short _currentBlockId = 1;
        public short CurrentBlockId
        {
            get { return _currentBlockId; }
            set
            {
                _currentBlockId = value;
                OnCurrentBlockChanged?.Invoke(_currentBlockId);
            }
        }

        public Action<short> OnCurrentBlockChanged;

        private float _cameraSpeed = 2.5f;
        private float _shiftSpeed = 5.5f;

        private float _sensitivity = 0.002f;
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;
        public bool CanMove = true;

        private SpotLight _spotLight;

        private InertiaController _inertiaController = new InertiaController();
        private CameraSway _cameraSway = new CameraSway();

        public Astronaut(Vector3 position, float aspectRatio)
            : base(position, aspectRatio)
        {
            _spotLight = new SpotLight(new Shader("Shaders/lighting"), Front);
            FOV = MathHelper.DegreesToRadians(90);
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);

            SetInertia();
            SetCameraSway();

            SetData();
        }

        public Astronaut(Vector3 position, float aspectRatio, Shader shader)
            : base(position, aspectRatio)
        {
            FOV = MathHelper.DegreesToRadians(90);
            _spotLight = new SpotLight(shader, Front);
            _spotLight.IsActive = false;
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);

            SetInertia();
            SetCameraSway();

            SetData();
        }

        ~Astronaut() {
            GameConsole.OnVisibilityWasChanged -= OnGameConsole;
        }

        private void SetData()
        {
            GameConsole.OnVisibilityWasChanged += OnGameConsole;
        }

        private void OnGameConsole(bool state)
        {
            CanMove = !state;
        }

        private void SetInertia()
        {
            _inertiaController.Enabled = true;
            _inertiaController.DecelerationRate = 1.5f;
            _inertiaController.MaxSpeed = 10.0f;
            _inertiaController.AccelerationRate = 4.0f;
        }

        private void SetCameraSway()
        {
            _cameraSway.InitialIntensity = 0.0015f;
            _cameraSway.MaxIntensity = 0.003f;

            _cameraSway.InitialFrequency = 15f;
            _cameraSway.MaxFrequency = 50f;

            _cameraSway.SpeedThreshold = 5.0f;
         
            _cameraSway.Enabled = true;
        }

        public override void OnCollisionEnter(Collision other)
        {
            Console.WriteLine($"Astronaut collided with {other.GetType().Name}");
        }

        public override void OnCollisionExit(Collision other)
        {
            Console.WriteLine($"Astronaut stopped colliding with {other.GetType().Name}");
        }

        public new void Update()
        {

            if (!CanMove) return;

            if (Input.IsKeyDown(Keys.F))
            {
                _spotLight.IsActive = !_spotLight.IsActive;
            }

            if (Input.IsKeyDown(Keys.I))
            {
                EnableInertia(!_inertiaController.Enabled);
            }

            if (Input.IsKeyDown(Keys.C))
            {
                EnableCameraSway(!_cameraSway.Enabled);
            }

            HandleInput();
            UpdateBounding();

            base.Update();
        }

        public void EnableInertia(bool enabled)
        {
            _inertiaController.EnableInertia(enabled);
        }

        public void SetInertiaParameters(float accelerationRate, float decelerationRate, float maxSpeed)
        {
            _inertiaController.SetParameters(accelerationRate, decelerationRate, maxSpeed);
        }

        public void EnableCameraSway(bool enabled)
        {
            _cameraSway.EnableSway(enabled);
        }

        public void SetCameraSwayParameters(float initialIntensity, float initialFrequency, float maxIntensity, float maxFrequency, float speedThreshold, float intensityRampRate = 2.0f, float frequencyRampRate = 2.0f)
        {
            _cameraSway.SetParameters(initialIntensity, initialFrequency, maxIntensity, maxFrequency, speedThreshold, intensityRampRate, frequencyRampRate);
        }

        public override Matrix4 GetViewMatrix()
        {
            Quaternion sway = _cameraSway.GetSwayRotation();
            Quaternion combinedRotation = sway * GetRotation();
            Vector3 newFront = Vector3.Transform(-Vector3.UnitZ, combinedRotation);
            Vector3 newUp = Vector3.Transform(Vector3.UnitY, combinedRotation);
            return Matrix4.LookAt(Position, Position + newFront, newUp);
        }

        private void HandleInput()
        {
            Vector3 acceleration = Vector3.Zero;
            bool isMoving = false;

            if (Input.IsKey(Keys.W))
            {
                acceleration += Front;
                isMoving = true;
            }
            if (Input.IsKey(Keys.S))
            {
                acceleration -= Front;
                isMoving = true;
            }
            if (Input.IsKey(Keys.A))
            {
                acceleration -= Right;
                isMoving = true;
            }
            if (Input.IsKey(Keys.D))
            {
                acceleration += Right;
                isMoving = true;
            }
            if (Input.IsKey(Keys.Space))
            {
                acceleration += Up;
                isMoving = true;
            }
            if (Input.IsKey(Keys.LeftControl))
            {
                acceleration -= Up;
                isMoving = true;
            }

            float currentSpeed = _cameraSpeed;
            if (Input.IsKey(Keys.LeftShift))
            {
                currentSpeed = _shiftSpeed;
            }

            float deltaTime = (float)Time.Delta;

            if (_inertiaController.Enabled)
            {
                if (isMoving)
                {
                    _inertiaController.ApplyInput(acceleration, currentSpeed, deltaTime);
                }
                _inertiaController.Update(deltaTime);
                Vector3 velocity = _inertiaController.Velocity;

                if (velocity != Vector3.Zero)
                {
                    Vector3 newPosition = Position + velocity * deltaTime;
                    BoundingVolume newBounding = GetBoundingVolumeAt(newPosition);

                    if (!CollisionManager.IsColliding(newBounding, this))
                    {
                        Position = newPosition;
                        UpdateBounding();
                        CollisionManager.Update(this, CollisionManager.GetBoundingVolume(this));
                    }
                }

                _cameraSway.Update(_inertiaController.Velocity.Length, deltaTime);
            }
            else
            {
                Vector3 movement = Vector3.Zero;

                if (Input.IsKey(Keys.W))
                {
                    movement += Front * currentSpeed * deltaTime;
                }
                if (Input.IsKey(Keys.S))
                {
                    movement -= Front * currentSpeed * deltaTime;
                }
                if (Input.IsKey(Keys.A))
                {
                    movement -= Right * currentSpeed * deltaTime;
                }
                if (Input.IsKey(Keys.D))
                {
                    movement += Right * currentSpeed * deltaTime;
                }
                if (Input.IsKey(Keys.Space))
                {
                    movement += Up * currentSpeed * deltaTime;
                }
                if (Input.IsKey(Keys.LeftControl))
                {
                    movement -= Up * currentSpeed * deltaTime;
                }

                if (movement != Vector3.Zero)
                {
                    Vector3 newPosition = Position + movement;
                    BoundingVolume newBounding = GetBoundingVolumeAt(newPosition);

                    if (!CollisionManager.IsColliding(newBounding, this))
                    {
                        Position = newPosition;
                        UpdateBounding();
                        CollisionManager.Update(this, CollisionManager.GetBoundingVolume(this));
                    }
                }
            }

            float roll = 1000f * deltaTime;

            if (Input.IsKey(Keys.Q))
            {
                Roll(-roll);
            }
            if (Input.IsKey(Keys.E))
            {
                Roll(roll);
            }

            if (Input.IsKeyDown(Keys.P))
            {
                Console.WriteLine("Saving Player");
                PlayerSaveLoadManager.SavePlayer(this);
            }

            if (Input.IsKeyDown(Keys.F5))
            {
                Debug.ShowPlayerCollision = !Debug.ShowPlayerCollision;

                if (Debug.ShowPlayerCollision)
                {
                    Debug.AddCollisionToDraw(this);
                }
                else
                {
                    Debug.RemoveCollisionToDraw(this);
                }
            }

            var mouse = Input.Mouse;

            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
                _firstMove = false;
            }
            else
            {
                if (CameraActive)
                {
                    var deltaX = mouse.Position.X - _lastMousePosition.X;
                    var deltaY = mouse.Position.Y - _lastMousePosition.Y;
                    _lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);

                    Rotate(deltaX, deltaY);
                }
            }

            if (Input.IsMouseButtonDown(MouseButton.Button1))
            {
                Shoot(100f);
            }

            if (_ray != null)
            {
                Debug.DrawRay(_ray, Color4.Red);
            }
        }

        private Ray _ray;

        public void Shoot(float rayLength)
        {
            _ray = new Ray(Position, Front, rayLength);
            CollisionLayer ignoreLayers = CollisionLayer.Player | CollisionLayer.Projectile;
            CollisionLayer layerMask = CollisionLayer.All & ~ignoreLayers;

            if (CollisionManager.Raycast(_ray, out Vector3 hitPosition, out Collision hitObject, layerMask))
            {
                Console.WriteLine($"Hit {hitObject.Name} at position {hitPosition}");
                Debug.DrawRay(_ray, Color4.Red);
                Debug.DrawBoundingSphere(new BoundingSphere(hitPosition, 0.1f), Color4.Red);
            }
        }

        public void Draw(Camera camera)
        {
            //_spotLight.Draw(this);
        }
    }
}
