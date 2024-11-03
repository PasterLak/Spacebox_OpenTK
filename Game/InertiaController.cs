using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class InertiaController
    {
        private Vector3 _velocity = Vector3.Zero;
        private bool _enabled = true;
        private float _decelerationRate = 1.0f;
        private float _maxSpeed = 10.0f;

        public Vector3 Velocity => _velocity;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public float DecelerationRate
        {
            get => _decelerationRate;
            set => _decelerationRate = value;
        }

        public float MaxSpeed
        {
            get => _maxSpeed;
            set => _maxSpeed = MathHelper.Clamp(value, 0.1f, 100.0f);
        }

        /// <summary>
        /// Применяет ускорение к текущей скорости.
        /// </summary>
        /// <param name="acceleration">Направление ускорения.</param>
        /// <param name="speed">Скорость ускорения.</param>
        /// <param name="deltaTime">Время между кадрами.</param>
        public void ApplyInput(Vector3 acceleration, float speed, float deltaTime)
        {
            if (acceleration.LengthSquared > 0)
            {
                acceleration = Vector3.Normalize(acceleration);
                _velocity += acceleration * speed * deltaTime;

                // Ограничение максимальной скорости
                if (_velocity.Length > _maxSpeed)
                {
                    _velocity = Vector3.Normalize(_velocity) * _maxSpeed;
                }
            }
        }

        /// <summary>
        /// Обновляет скорость, применяя инерцию.
        /// </summary>
        /// <param name="deltaTime">Время между кадрами.</param>
        public void Update(float deltaTime)
        {
            if (_enabled && _velocity.Length > 0)
            {
                float damping = 1.0f - _decelerationRate * deltaTime;
                damping = MathHelper.Clamp(damping, 0.0f, 1.0f);
                _velocity *= damping;

                if (_velocity.Length < 0.01f)
                {
                    _velocity = Vector3.Zero;
                }
            }
        }

        /// <summary>
        /// Устанавливает текущую скорость.
        /// </summary>
        /// <param name="velocity">Новая скорость.</param>
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        /// <summary>
        /// Сбрасывает скорость к нулю.
        /// </summary>
        public void Reset()
        {
            _velocity = Vector3.Zero;
        }
    }
}
