using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class InertiaController
    {
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        private bool _enabled = true;

        public float WalkAccelerationRate { get; set; } = 10.0f;
        public float RunAccelerationRate { get; set; } = 20.0f;

        public float DecelerationRate { get; set; } = 5.0f;

        public float WalkMaxSpeed { get; set; } = 10.0f;
        public float RunMaxSpeed { get; set; } = 20.0f;

        public float MaxSpeed { get; set; } = 10.0f;
        public float AccelerationRate { get; set; } = 10.0f;

        public bool Enabled { get => _enabled; set => _enabled = value; }

        public void ApplyInput(Vector3 direction, float deltaTime)
        {
            if (direction.LengthSquared > 0)
            {
                direction = Vector3.Normalize(direction);
                Velocity += direction * AccelerationRate * deltaTime;
                if (Velocity.Length > MaxSpeed)
                {
                    Velocity = Vector3.Normalize(Velocity) * MaxSpeed;
                }
            }
        }

        public void Update(bool isMoving, float deltaTime)
        {
            if (_enabled)
            {
                if (!isMoving && Velocity.Length > 0)
                {
                    
                    float speed = Velocity.Length;
                    float decelerationAmount = DecelerationRate * deltaTime;
                    if (speed <= decelerationAmount)
                    {
                        Velocity = Vector3.Zero;
                    }
                    else
                    {
                        Velocity -= Velocity.Normalized() * decelerationAmount;
                    }
                }
            }
        }

        public void SetParameters(float walkAccelerationRate, float runAccelerationRate, float decelerationRate, float walkMaxSpeed, float runMaxSpeed)
        {
            WalkAccelerationRate = walkAccelerationRate;
            RunAccelerationRate = runAccelerationRate;
            DecelerationRate = decelerationRate;
            WalkMaxSpeed = walkMaxSpeed;
            RunMaxSpeed = runMaxSpeed;
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
            Velocity = Vector3.Zero;
        }
    }
}
