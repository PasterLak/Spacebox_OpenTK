using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;

namespace Spacebox.Game
{
    public class Astronaut : Camera360, INotTransparent
    {

        public string Name { get; private set; } = "Player";

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

        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }

        private float _cameraSpeed = 2.5f;
        private float _shiftSpeed = 5.5f;

        private float _sensitivity = 0.002f;
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;

        private bool _canMove = true;
        public bool CanMove
        {
            get
            {
                return _canMove;
            }
            set
            {
                _canMove = value;
                _lastMousePosition = Input.Mouse.Position;
            }
        }

        public SpotLight Flashlight { get; private set; }

        private InertiaController _inertiaController = new InertiaController();
        private CameraSway _cameraSway = new CameraSway();
        private AudioSource flashlightOn;
        private AudioSource flashlightOff;

        public Astronaut(Vector3 position)
            : base(position)
        {
            Flashlight = new SpotLight(ShaderManager.GetShader("Shaders/block"), Front);
            Flashlight.UseSpecular = false;
            FOV = MathHelper.DegreesToRadians(90);
            Layer = CollisionLayer.Player;
            VisualDebug.RemoveCollisionToDraw(this);

            SetInertia();
            SetCameraSway();

            SetData();
        }

        public Astronaut(Vector3 position, Shader shader)
            : base(position)
        {
            FOV = MathHelper.DegreesToRadians(90);
            Flashlight = new SpotLight(shader, Front);
            Flashlight.UseSpecular = false;
            Layer = CollisionLayer.Player;
            VisualDebug.RemoveCollisionToDraw(this);

            SetInertia();
            SetCameraSway();

            SetData();
        }

        ~Astronaut() {
            Debug.OnVisibilityWasChanged -= OnGameConsole;
        }

        private void SetData()
        {
            Debug.OnVisibilityWasChanged += OnGameConsole;

            Inventory = new Inventory(8,5);
            Panel = new Storage(1,10);

            Inventory.Name = "Inventory";
            Panel.Name = "Panel";

            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);

            flashlightOn = 

            /*
            Panel.TryAddItem(GameBlocks.GetItemByName("Drill"), 1);
            Panel.TryAddItem(GameBlocks.GetItemByName("Weapone"), 1);
            Panel.TryAddItem(GameBlocks.GetItemByName("Titanium Ingot"), 52);
            Panel.TryAddItem(GameBlocks.GetItemByName("Iron Lens"), 8);

            Inventory.TryAddItem(GameBlocks.GetItemByName("Titanium Heavy Block"), 42);
            Inventory.TryAddItem(GameBlocks.GetItemByName("AI Core"), 12);
            */
        }

        private void OnGameConsole(bool state)
        {
            CanMove = !state;

            _lastMousePosition = Input.Mouse.Position;
        }

        private void SetInertia()
        {
            _inertiaController.Enabled = true;
            _inertiaController.DecelerationRate = 1.5f;
            _inertiaController.MaxSpeed = 15.0f;
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

        public static void PrintMatrix(Matrix4 matrix)
        {
            Console.WriteLine("Matrix4:");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine($"{matrix[i, 0]}, {matrix[i, 1]}, {matrix[i, 2]}, {matrix[i, 3]}");
            }
            Console.WriteLine();
        }

        public override void Update()
        {
            Matrix4 viewMatrix = GetViewMatrix();
            Matrix4 projectionMatrix = GetProjectionMatrix();
            Frustum.UpdateFrustum(viewMatrix, projectionMatrix);

            VisualDebug.ProjectionMatrix = projectionMatrix;
            VisualDebug.ViewMatrix = viewMatrix;


            if (VisualDebug.ShowDebug)
            {
                VisualDebug.ProjectionMatrix = GetProjectionMatrix();
                VisualDebug.ViewMatrix = GetViewMatrix();
            }

           


            if (!CanMove) return;

            if (Input.IsKeyDown(Keys.F))
            {
                Flashlight.IsActive = !Flashlight.IsActive;
            }

            if (Input.IsKeyDown(Keys.O))
            {
                EnableInertia(!_inertiaController.Enabled);
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
            Quaternion combinedRotation = GetRotation() * sway;
            Vector3 newFront = Vector3.Transform(-Vector3.UnitZ, combinedRotation);
            Vector3 newUp = Vector3.Transform(Vector3.UnitY, combinedRotation);

            return Matrix4.LookAt(Position, Position + newFront, newUp);
        }



        private void HandleInput()
        {
            Vector3 acceleration = Vector3.Zero;
            bool isMoving = false;

            if (Input.MouseScrollDelta.Y < 0)
            {
                CurrentBlockId++;



                if (CurrentBlockId > GameBlocks.MaxBlockId)
                {
                    CurrentBlockId = 1;
                }

            }

            if (Input.MouseScrollDelta.Y > 0)
            {
                CurrentBlockId--;

                if (CurrentBlockId < 1)
                {
                    CurrentBlockId = GameBlocks.MaxBlockId;
                }


            }

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
                _inertiaController.Update();
                if (isMoving)
                {
                    _inertiaController.ApplyInput(acceleration, currentSpeed, deltaTime);
                }
                
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
                VisualDebug.ShowPlayerCollision = !VisualDebug.ShowPlayerCollision;

                if (VisualDebug.ShowPlayerCollision)
                {
                    VisualDebug.AddCollisionToDraw(this);
                }
                else
                {
                    VisualDebug.RemoveCollisionToDraw(this);
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
                VisualDebug.DrawRay(_ray, Color4.Red);
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
                VisualDebug.DrawRay(_ray, Color4.Red);
                VisualDebug.DrawBoundingSphere(new BoundingSphere(hitPosition, 0.1f), Color4.Red);
            }
        }
        public void Draw()
        {
            Draw(this);
        }
        public void Draw(Camera camera)
        {
            Flashlight.Draw(this);
        }
    }
}
