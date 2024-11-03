using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class InertiaController
    {
        private Vector3 _velocity = Vector3.Zero;
        private bool _enabled = true;
        private float _accelerationRate = 10.0f;
        private float _decelerationRate = 5.0f;
        private float _maxSpeed = 10.0f;

        public Vector3 Velocity => _velocity;
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public float AccelerationRate { get => _accelerationRate; set => _accelerationRate = value; }
        public float DecelerationRate { get => _decelerationRate; set => _decelerationRate = value; }
        public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = MathHelper.Clamp(value, 0.1f, 100.0f); }

        public void ApplyInput(Vector3 direction, float speed, float deltaTime)
        {
            if (direction.LengthSquared > 0)
            {
                direction = Vector3.Normalize(direction);
                _velocity += direction * speed * _accelerationRate * deltaTime;
                if (_velocity.Length > _maxSpeed)
                {
                    _velocity = Vector3.Normalize(_velocity) * _maxSpeed;
                }
            }
        }

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

        public void SetParameters(float accelerationRate, float decelerationRate, float maxSpeed)
        {
            _accelerationRate = accelerationRate;
            _decelerationRate = decelerationRate;
            _maxSpeed = MathHelper.Clamp(maxSpeed, 0.1f, 100.0f);
        }

        public void EnableInertia(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
            {
                Reset();
            }
        }

        public void Reset()
        {
            _velocity = Vector3.Zero;
        }
    }
}
