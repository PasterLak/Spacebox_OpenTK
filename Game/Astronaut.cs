using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.GUI;
using Spacebox.Scenes;

namespace Spacebox.Game
{
    public class Astronaut : Camera360, INotTransparent
    {
        public string Name { get; private set; } = "Player";
        public Inventory Inventory { get; private set; }
        public Storage Panel { get; private set; }

        private float _cameraSpeed = 2.5f;
        private float _shiftSpeed = 5.5f;

        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;

        private bool _canMove = true;
        public bool CanMove
        {
            get => _canMove;
            set
            {
                _canMove = value;
                _lastMousePosition = Input.Mouse.Position;
            }
        }

        public SpotLight Flashlight { get; private set; }

        public InertiaController InertiaController { get; private set; } = new InertiaController();
        private CameraSway _cameraSway = new CameraSway();

        private bool _collisionEnabled = true;
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set => _collisionEnabled = value;
        }

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

        ~Astronaut()
        {
            Debug.OnVisibilityWasChanged -= OnGameConsole;
        }

        private void SetData()
        {
            Debug.OnVisibilityWasChanged += OnGameConsole;

            Inventory = new Inventory(8, 4);
            Panel = new Storage(1, 10);
            BoundingVolume = new BoundingSphere(Position, 0.4f);
            Inventory.Name = "Inventory";
            Panel.Name = "Panel";

            Panel.ConnectStorage(Inventory, true);
            Inventory.ConnectStorage(Panel);
        }

        private void OnGameConsole(bool state)
        {
            CanMove = !state;
            _lastMousePosition = Input.Mouse.Position;
        }

        private void SetInertia()
        {
            InertiaController.Enabled = true;
            InertiaController.SetParameters(
                walkTimeToMaxSpeed: 0.6f,
                 walkTimeToStop: 0.5f,
                 runTimeToMaxSpeed: 1.5f,
                  runTimeToStop: 0.4f,
               
                walkMaxSpeed: 8,
                runMaxSpeed: 20
            );



            InertiaController.SetMode(isRunning: false);
            InertiaController.MaxSpeed = InertiaController.WalkMaxSpeed;
            //InertiaController.AccelerationRate = InertiaController.WalkAccelerationRate;
            InertiaController.InertiaType = InertiaType.Damping;
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

            if (Input.IsKeyDown(Keys.I))
            {
                EnableInertia(!InertiaController.Enabled);
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
            if (Input.IsKeyDown(Keys.P))
            {
                Console.WriteLine("Saving Player");
                PlayerSaveLoadManager.SavePlayer(this);
            }

            if (Input.IsKeyDown(Keys.U))
            {
                CollisionEnabled = !CollisionEnabled;
                Debug.Log($"Collision Enabled: {CollisionEnabled}");
            }

            HandleInput();
            UpdateBounding();

            base.Update();
        }

        public void EnableInertia(bool enabled)
        {
            InertiaController.EnableInertia(enabled);
        }

        public void SetInertiaParameters(float walkAccelerationRate, float runAccelerationRate, float decelerationFactor, float walkMaxSpeed, float runMaxSpeed)
        {
           // InertiaController.SetParameters(walkAccelerationRate, runAccelerationRate, decelerationFactor, walkMaxSpeed, runMaxSpeed);
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

            float roll = 1000f * Time.Delta;

            if (Input.IsKey(Keys.Q))
            {
                Roll(-roll);
            }
            if (Input.IsKey(Keys.E))
            {
                Roll(roll);
            }

            float deltaTime = (float)Time.Delta;

            Vector3 movement = Vector3.Zero;

            bool isRunning = Input.IsKey(Keys.LeftShift);
            InertiaController.SetMode(isRunning);

            if (InertiaController.Enabled)
            {
               
                /*if (Input.IsKey(Keys.LeftShift))
                {
                    InertiaController.MaxSpeed = InertiaController.RunMaxSpeed;
                    //InertiaController.AccelerationRate = InertiaController.RunAccelerationRate;
                }
                else
                {
                    InertiaController.MaxSpeed = InertiaController.WalkMaxSpeed;
                    //InertiaController.AccelerationRate = InertiaController.WalkAccelerationRate;
                }*/

                if (isMoving)
                {
                    InertiaController.ApplyInput(acceleration, deltaTime);
                }

                InertiaController.Update(isMoving, deltaTime);

                movement = InertiaController.Velocity * deltaTime;

                _cameraSway.Update(InertiaController.Velocity.Length, deltaTime);
            }
            else
            {
                float currentSpeed = isRunning ? _shiftSpeed : _cameraSpeed;

                if (isMoving)
                {
                    movement = acceleration.Normalized() * currentSpeed * deltaTime;
                }

                _cameraSway.Update(movement.Length / deltaTime, deltaTime);
            }

            if (movement != Vector3.Zero)
            {
                MoveAndCollide(movement);
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

        public void MoveAndCollide(Vector3 movement)
        {
            Vector3 position = Position;

            if (CollisionEnabled)
            {
                Chunk chunk = Chunk.CurrentChunk;

                position.X += movement.X;
                UpdateBoundingAt(position);
                if (chunk.IsColliding(BoundingVolume))
                {
                    position.X -= movement.X;
                    ApplyVelocityDamage(InertiaController.Velocity.X);
                    InertiaController.Velocity = new Vector3(0, InertiaController.Velocity.Y, InertiaController.Velocity.Z);
                }
           

                position.Y += movement.Y;
                UpdateBoundingAt(position);
                if (chunk.IsColliding(BoundingVolume))
                {
                    position.Y -= movement.Y;
                    ApplyVelocityDamage(InertiaController.Velocity.Y);
                    InertiaController.Velocity = new Vector3(InertiaController.Velocity.X, 0, InertiaController.Velocity.Z);
                }

                position.Z += movement.Z;
                UpdateBoundingAt(position);
                if (chunk.IsColliding(BoundingVolume))
                {
                    position.Z -= movement.Z;
                    ApplyVelocityDamage(InertiaController.Velocity.Z);
                    InertiaController.Velocity = new Vector3(InertiaController.Velocity.X, InertiaController.Velocity.Y, 0);
                }

                Position = position;
            }
            else
            {
                Position += movement;
            }
        }

        private void ApplyVelocityDamage(float value)
        {
           
            if (Math.Abs(value) > 10)
            {

                float damage = (int)(Math.Abs(value) - 10f);
                Debug.Log($"Damaged from hitting a wall! Damage: {damage}  Speed: {value}");
                

                if(damage > 5)
                {
                    SpaceScene.Uii.Stop();
                    BlackScreenOverlay.IsEnabled = true;
                    CanMove = false;
                    Settings.ShowInterface = false;

                    SpaceScene.Death.Play();
                    SpaceScene.DeathOn = true;
                }
            }
        }

        private void UpdateBoundingAt(Vector3 position)
        {
            if (BoundingVolume is BoundingSphere sphere)
            {
                sphere.Center = position + Offset;
            }
            else if (BoundingVolume is BoundingBox box)
            {
                box.Center = position + Offset;
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
