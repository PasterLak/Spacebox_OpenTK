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

        private SpotLight _spotLight;

        private InertiaController _inertiaController = new InertiaController();

        public Astronaut(Vector3 position, float aspectRatio)
            : base(position, aspectRatio)
        {
            _spotLight = new SpotLight(new Shader("Shaders/lighting"), Front);
            FOV = MathHelper.DegreesToRadians(90);
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);

           
            _inertiaController.Enabled = true;
            _inertiaController.DecelerationRate = 0.5f;
            _inertiaController.MaxSpeed = 10.0f; 
        }

        public Astronaut(Vector3 position, float aspectRatio, Shader shader)
            : base(position, aspectRatio)
        {
            FOV = MathHelper.DegreesToRadians(90);
            _spotLight = new SpotLight(shader, Front);
            _spotLight.IsActive = false;
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);

            // Настройка инерции
            _inertiaController.Enabled = true;
            _inertiaController.DecelerationRate = 0.5f;
            _inertiaController.MaxSpeed = 10.0f;
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
            if (Input.IsKeyDown(Keys.F))
            {
                _spotLight.IsActive = !_spotLight.IsActive;
                // audio.Play();
            }

            HandleInput();
            UpdateBounding();

            base.Update();
        }

        /// <summary>
        /// Включает или выключает инерцию.
        /// </summary>
        /// <param name="enabled">Включить или выключить инерцию.</param>
        public void EnableInertia(bool enabled)
        {
            _inertiaController.Enabled = enabled;
        }

        /// <summary>
        /// Устанавливает параметры инерции.
        /// </summary>
        /// <param name="decelerationRate">Коэффициент замедления.</param>
        /// <param name="maxSpeed">Максимальная скорость.</param>
        public void SetInertiaParameters(float decelerationRate, float maxSpeed)
        {
            _inertiaController.DecelerationRate = decelerationRate;
            _inertiaController.MaxSpeed = maxSpeed;
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

            if (isMoving)
            {
                _inertiaController.ApplyInput(acceleration, currentSpeed, (float)Time.Delta);
            }

            _inertiaController.Update((float)Time.Delta);

            Vector3 velocity = _inertiaController.Velocity;

            if (velocity != Vector3.Zero)
            {
                Vector3 newPosition = Position + velocity * (float)Time.Delta;
                BoundingVolume newBounding = GetBoundingVolumeAt(newPosition);

                if (!CollisionManager.IsColliding(newBounding, this))
                {
                    Position = newPosition;
                    UpdateBounding();
                    CollisionManager.Update(this, CollisionManager.GetBoundingVolume(this));
                }
            }

            float roll = 1000f * (float)Time.Delta;

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
            else
            {
                // No hit detected
            }
        }

        public void Draw(Camera camera)
        {
            //_spotLight.Draw(this);
        }
    }
}
