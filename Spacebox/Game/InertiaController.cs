using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public enum InertiaType
    {
        Linear,
        Quadratic,
        Damping
    }


    public class InertiaController
    {
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        private bool _enabled = true;

        public float WalkMaxSpeed { get; set; } = 10.0f;
        public float RunMaxSpeed { get; set; } = 20.0f;
        public float MaxSpeed { get; set; } = 10.0f;

     
        public float WalkTimeToMaxSpeed { get; set; } = 1.0f;
        public float WalkTimeToStop { get; set; } = 1.0f;
        public float RunTimeToMaxSpeed { get; set; } = 0.5f;
        public float RunTimeToStop { get; set; } = 0.5f;

     
        private float _currentTimeToMaxSpeed;
        private float _currentTimeToStop;

        public InertiaType InertiaType { get; set; } = InertiaType.Linear;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

      
        public void SetMode(bool isRunning)
        {
            if (isRunning)
            {
                MaxSpeed = RunMaxSpeed;
                _currentTimeToMaxSpeed = RunTimeToMaxSpeed;
                _currentTimeToStop = RunTimeToStop;
            }
            else
            {
                MaxSpeed = WalkMaxSpeed;
                _currentTimeToMaxSpeed = WalkTimeToMaxSpeed;
                _currentTimeToStop = WalkTimeToStop;
            }
        }

        public void ApplyInput(Vector3 direction, float deltaTime)
        {
            if (direction.LengthSquared > 0)
            {
                direction = Vector3.Normalize(direction);

                switch (InertiaType)
                {
                    case InertiaType.Linear:
                        float linearAcceleration = MaxSpeed / _currentTimeToMaxSpeed;
                        Velocity += direction * linearAcceleration * deltaTime;
                        break;

                    case InertiaType.Quadratic:
                        float quadraticAcceleration = (2 * MaxSpeed) / (_currentTimeToMaxSpeed * _currentTimeToMaxSpeed);
                        Velocity += direction * quadraticAcceleration * deltaTime * deltaTime;
                        break;

                    case InertiaType.Damping:
                        float dampingFactor = 1 - (float)Math.Exp(-deltaTime / _currentTimeToMaxSpeed);
                        Vector3 desiredVelocity = direction * MaxSpeed;
                        Velocity += (desiredVelocity - Velocity) * dampingFactor;
                        break;
                }

              
                if (Velocity.Length > MaxSpeed)
                {
                    Velocity = Vector3.Normalize(Velocity) * MaxSpeed;
                }
            }
        }

        public void Update(bool isMoving, float deltaTime)
        {
            if (!_enabled) return;

            if (!isMoving && Velocity.Length > 0)
            {
                switch (InertiaType)
                {
                    case InertiaType.Linear:
                        float linearDeceleration = MaxSpeed / _currentTimeToStop;
                        float decelerationAmount = linearDeceleration * deltaTime;
                        if (Velocity.Length <= decelerationAmount)
                        {
                            Velocity = Vector3.Zero;
                        }
                        else
                        {
                            Velocity -= Vector3.Normalize(Velocity) * decelerationAmount;
                        }
                        break;

                    case InertiaType.Quadratic:
                        float quadraticDeceleration = (2 * MaxSpeed) / (_currentTimeToStop * _currentTimeToStop);
                        float decelerationQuadratic = quadraticDeceleration * deltaTime * deltaTime;
                        if (Velocity.LengthSquared <= decelerationQuadratic * decelerationQuadratic)
                        {
                            Velocity = Vector3.Zero;
                        }
                        else
                        {
                            Velocity -= Vector3.Normalize(Velocity) * decelerationQuadratic;
                        }
                        break;

                    case InertiaType.Damping:
                        float dampingFactor = (float)Math.Exp(-deltaTime / _currentTimeToStop);
                        Velocity *= dampingFactor;
                        if (Velocity.LengthSquared < 0.0001f)
                        {
                            Velocity = Vector3.Zero;
                        }
                        break;
                }
            }
        }

        public void SetParameters(
            float walkTimeToMaxSpeed, float walkTimeToStop,
            float runTimeToMaxSpeed, float runTimeToStop,
            float walkMaxSpeed, float runMaxSpeed)
        {
            WalkTimeToMaxSpeed = walkTimeToMaxSpeed;
            WalkTimeToStop = walkTimeToStop;
            RunTimeToMaxSpeed = runTimeToMaxSpeed;
            RunTimeToStop = runTimeToStop;
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
