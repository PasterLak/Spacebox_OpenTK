using OpenTK.Mathematics;

namespace Engine.Animation
{
    public class RotateAnimation : IAnimation
    {
        private float _anglePerSecond;
        private float _currentAngle;
        private Vector3 _axis; // normalized, for example, (0,1,0)

        private float _duration; 
        private float _elapsedTime;

        public RotateAnimation(Vector3 axis, float anglePerSecond, float duration = 0f) // 0 no stop
        {
            _axis = axis;
            _anglePerSecond = anglePerSecond;
            _duration = duration;
        }

        public void Init()
        {
            _currentAngle = 0f;
            _elapsedTime = 0f;
        }

        public bool Update(float deltaTime)
        {
            _elapsedTime += deltaTime;


            if (_duration > 0f && _elapsedTime >= _duration)
            {
                return false;
            }

            _currentAngle += _anglePerSecond * deltaTime;

            return true; 
        }

        public void Apply(Node3D node)
        {
            if (node == null) return;

            var rot = node.Rotation;

            if (_axis == Vector3.UnitY)
            {
                rot.Y = _currentAngle;
            }
            if (_axis == Vector3.UnitX)
            {
                rot.X = _currentAngle;
            }
            if (_axis == Vector3.UnitZ)
            {
                rot.Z = _currentAngle;
            }
            node.Rotation = rot;
        }

        public void Reset()
        {
            _currentAngle = 0f;
            _elapsedTime = 0f;
        }
    }
}
