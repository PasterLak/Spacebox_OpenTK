using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;

namespace Spacebox.Entities
{
    public class Player : ICollidable
    {
        public Camera Camera { get; private set; }
        public Transform Transform { get; private set; }

        private float _cameraSpeed = 1.5f;
        private float _sensitivity = 0.2f;
        private bool _firstMove = true;
        private Vector2 _lastMousePosition;

        private BoundingSphere _sphere;
        public BoundingSphere BoundingSphere
        {
            get
            {
                _sphere.Center = Camera.Position;
                return _sphere;
            }
            private set
            {
                _sphere = value;
            }
        }

        public BoundingVolume BoundingVolume => _sphere;

        public bool IsStatic => false;

        public Player(Vector3 position, float aspectRatio)
        {
            Transform = new Transform();
            Transform.Position = position;
            Camera = new Camera(position, aspectRatio);

            BoundingSphere = new BoundingSphere(position, 1);
        }

        public void Update()
        {
            HandleInput();

            //Debug.DrawBoundingBox(new BoundingBox(Camera.Position, Vector3.One), Color4.Green);
            Debug.DrawBoundingSphere(BoundingSphere, Color4.Green);
            Debug.DrawBoundingSphere(new BoundingSphere(new Vector3(-3,5,-6), 1), Color4.Green);
        }

        private void HandleInput()
        {
           
            var mouse = Input.MouseState;



            if (Input.IsKey(Keys.W))
            {
                Camera.Position += Camera.Front * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.S))
            {
                Camera.Position -= Camera.Front * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.A))
            {
                Camera.Position -= Camera.Right * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.D))
            {
                Camera.Position += Camera.Right * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.Space))
            {
                Camera.Position += Camera.Up * _cameraSpeed * (float)Time.Delta;
            }
            if (Input.IsKey(Keys.LeftShift))
            {
                Camera.Position -= Camera.Up * _cameraSpeed * (float)Time.Delta;
            }

            if (_firstMove)
            {
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastMousePosition.X;
                var deltaY = mouse.Y - _lastMousePosition.Y;
                _lastMousePosition = new Vector2(mouse.X, mouse.Y);

                Camera.Yaw += deltaX * _sensitivity;
                Camera.Pitch -= deltaY * _sensitivity;
            }
        }

        public void OnCollision(ICollidable other)
        {
            // Реализуйте логику реакции на коллизии здесь
            Console.WriteLine($"{this} on collision with {other}");

            // Пример: изменение цвета объекта при коллизии
            // (предполагается, что у вас есть соответствующий метод или свойство)
             // Красный цвет
        }

        public void UpdateBounding()
        {
           
              
        }

        private HashSet<ICollidable> _currentColliders = new HashSet<ICollidable>();
        public void OnCollisionEnter(ICollidable other)
        {
            Console.WriteLine($"Camera collided with {other}");
            // Реализуйте логику реакции камеры на коллизии здесь
            // Например, остановка движения или изменение позиции
        }

        /// <summary>
        /// Метод вызывается при окончании коллизии с другим объектом.
        /// </summary>
        /// <param name="other">Объект, с которым окончилась коллизия.</param>
        public void OnCollisionExit(ICollidable other)
        {
            Console.WriteLine($"Camera stopped colliding with {other}");
            // Реализуйте логику реакции камеры на окончание коллизии здесь
        }

    }
}
