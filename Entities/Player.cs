using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;


namespace Spacebox.Entities
{
    public class Player : CameraBasic, INotTransparent
    {
        private float _cameraSpeed = 2.5f;
        private float _shiftSpeed = 5.5f;

        private float _sensitivity = 0.2f;
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        public bool CameraActive = true;

        SpotLight spotLight;

        public Player(Vector3 position, float aspectRatio)
            : base(position, aspectRatio)
        {
            spotLight = new SpotLight(new Shader("Shaders/lighting"), Front);

            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);
        }

        public Player(Vector3 position, float aspectRatio, Shader shader)
            : base(position, aspectRatio)
        {
            spotLight = new SpotLight(shader, Front);
            spotLight.IsActive = false;
            Layer = CollisionLayer.Player;
            Debug.RemoveCollisionToDraw(this);
        }

        public override void OnCollisionEnter(Collision other)
        {
            Console.WriteLine($"Camera collided with {other.GetType().Name}");
        }

        public override void OnCollisionExit(Collision other)
        {
            Console.WriteLine($"Camera stopped colliding with {other.GetType().Name}");
        }

        public new void Update()
        {
            if (Input.IsKeyDown(Keys.F))
            {
                spotLight.IsActive = !spotLight.IsActive;
                //audio.Play();
            }

            

            HandleInput();
            UpdateBounding();

            base.Update();
           
        }

        private float currentSpeed = 0;

        float jump = 0;
        private void HandleInput()
        {
            var mouse = Input.Mouse;

            currentSpeed = _cameraSpeed;

            if (Input.IsKey(Keys.LeftShift))
            {
                currentSpeed = _shiftSpeed;
            }

            Vector3 movement = Vector3.Zero;

            if (Input.IsKeyDown(Keys.J) && jump == 0)
            {
                
               // jump = 2;

               
            }

            if(jump > 0)
            {
                movement = new Vector3(0, 0.1f, 0);
                jump -= 10f * Time.Delta;

                if (jump < 0) jump = 0;
            }
            else
            {
               // movement = new Vector3(0, -0.1f, 0);
            }

            //Debug.DrawRay( new Ray(new Vector3(3,3,3), Vector3.UnitY, 10), Color4.Red);
            if(Input.Mouse.IsButtonDown(MouseButton.Button1))
            {

                Shoot(10);
            }
            Debug.DrawRay(ray, Color4.Red);

            if (Input.IsKey(Keys.W))
            {
                movement += Front * currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.S))
            {
                movement -= Front * currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.A))
            {
                movement -= Right * currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.D))
            {
                movement += Right * currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.Space))
            {
                movement += Up * currentSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.LeftControl))
            {
                movement -= Up * currentSpeed * (float)Time.Delta;
            }

            if( Input.IsKeyDown(Keys.F5))
            {
                Debug.ShowPlayerCollision = !Debug.ShowPlayerCollision;

                if(Debug.ShowPlayerCollision)
                {
                    Debug.AddCollisionToDraw(this);
                }
                else
                {
                    Debug.RemoveCollisionToDraw(this);
                }

            }

            if (movement != Vector3.Zero)
            {
                Vector3 newPosition = Position + movement;
                BoundingVolume newBounding = GetBoundingVolumeAt(newPosition);

                if (!CollisionManager.IsColliding(newBounding, this))
                {
                   
                    Position = newPosition;
                    UpdateBounding();
                    CollisionManager.Update(this, CollisionManager.GetBoundingVolume(this)); // Предполагается, что есть метод для получения текущего BoundingVolume
                }
                else
                {
                    // Коллизия обнаружена, движение отменяется или корректируется
                    //Console.WriteLine("Movement blocked by collision.");
                }
            }

            
            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                if(CameraActive)
                {
                    var deltaX = mouse.X - _lastMousePosition.X;
                    var deltaY = mouse.Y - _lastMousePosition.Y;
                    _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                    Yaw += deltaX * _sensitivity;
                    Pitch -= deltaY * _sensitivity;


                    Pitch = MathHelper.Clamp(Pitch, -89f, 89f);
                }
          
            }
        }

        Ray ray;
        public void Shoot(float rayLength)
        {
          
             ray = new Ray(Position, Front, rayLength);
            CollisionLayer ignoreLayers =
                    CollisionLayer.Player | CollisionLayer.Projectile;
            CollisionLayer layerMask = CollisionLayer.All & ~ignoreLayers;

            if (CollisionManager.Raycast(ray, out Vector3 hitPosition,
                out Collision hitObject, layerMask))
            {
                Console.WriteLine($"Hit {hitObject.Name} at position {hitPosition}");
               // hitObject.Position = new Vector3(0,-100,0);
                Debug.DrawRay(ray, Color4.Red);
                Debug.DrawBoundingSphere(new BoundingSphere(hitPosition, 0.1f), Color4.Red);
            }
            else
            {
                Console.WriteLine("No hit detected.");
            }
        }



        public void Draw(Camera camera)
        {
            
            spotLight.Draw(this);
        }
    }
}
