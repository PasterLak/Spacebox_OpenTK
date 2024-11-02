using OpenTK.Mathematics;

using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;

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

        private float _sensitivity = 0.002f; // Изменил чувствительность для соответствия с методом Rotate
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;

        private SpotLight _spotLight;

        public Astronaut(Vector3 position, float aspectRatio)
            : base(position, aspectRatio)
        {
            _spotLight = new SpotLight(new Shader("Shaders/lighting"), Front);
            FOV = MathHelper.DegreesToRadians(90);
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);
        }

        public Astronaut(Vector3 position, float aspectRatio, Shader shader)
            : base(position, aspectRatio)
        {
            FOV =  MathHelper.DegreesToRadians(90);
            _spotLight = new SpotLight(shader, Front);
            _spotLight.IsActive = false;
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);
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

        private float _currentSpeed = 0f;

        private void HandleInput()
        {
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

            bool isAltPressed = Input.IsKey(Keys.LeftAlt) || Input.IsKey(Keys.RightAlt);

            if (isAltPressed)
            {
                
                return;
            }

            var mouse = Input.Mouse;

            _currentSpeed = _cameraSpeed;

            if (Input.IsKey(Keys.LeftShift))
            {
                _currentSpeed = _shiftSpeed;
            }

            Vector3 movement = Vector3.Zero;

            // Обработка движения вперед, назад, влево, вправо, вверх, вниз
            if (Input.IsKey(Keys.W))
            {
                movement += Front * _currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.S))
            {
                movement -= Front * _currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.A))
            {
                movement -= Right * _currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.D))
            {
                movement += Right * _currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.Space))
            {
                movement += Up * _currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.LeftControl))
            {
                movement -= Up * _currentSpeed * (float)Time.Delta;
            }

            var roll = 1000f * Time.Delta;
            // Обработка вращения вокруг оси Z (ролл)
            if (Input.IsKey(Keys.Q))
            {
                Roll(-roll);
            }
            if (Input.IsKey(Keys.E))
            {
                Roll(roll);
            }

            if(Input.IsKeyDown(Keys.P))
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

            // Обработка движения с учетом коллизий
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
                else
                {
                    // Обработка столкновения
                    // Console.WriteLine("Movement blocked by collision.");
                }
            }

            // Обработка вращения камеры с помощью мыши
            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                if (CameraActive)
                {
                    var deltaX = mouse.X - _lastMousePosition.X;
                    var deltaY = mouse.Y - _lastMousePosition.Y;
                    _lastMousePosition = new Vector2(mouse.X, mouse.Y);

                    // Используем метод Rotate из новой реализации камеры
                    Rotate(deltaX, deltaY);
                }
            }

            // Обработка стрельбы
            if (Input.Mouse.IsButtonDown(MouseButton.Button1))
            {
                Shoot(100f);
            }

            // Рисуем луч стрельбы для отладки
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
                // hitObject.Position = new Vector3(0, -100, 0);
                Debug.DrawRay(_ray, Color4.Red);
                Debug.DrawBoundingSphere(new BoundingSphere(hitPosition, 0.1f), Color4.Red);
            }
            else
            {
                //Console.WriteLine("No hit detected.");
            }
        }

        public void Draw(Camera camera)
        {
            //_spotLight.Draw(this);
        }
    }
}
