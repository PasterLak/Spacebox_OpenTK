using OpenTK.Mathematics;

namespace Engine.Animation
{
    public class MoveAnimation : IAnimation
    {
        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _duration;

        private float _elapsedTime;
        private float _progress01; 

        private bool _loop; 

        public MoveAnimation(Vector3 startPos, Vector3 endPos, float duration, bool loop = false)
        {
            _startPos = startPos;
            _endPos = endPos;
            _duration = duration;
            _loop = loop;
        }

        public void Init()
        {
            _elapsedTime = 0f;
            _progress01 = 0f;
        }

        public bool Update(float deltaTime)
        {
            _elapsedTime += deltaTime;
            if (_duration <= 0)
                return true; // o is no stop

            _progress01 = _elapsedTime / _duration;

            if (_progress01 > 1f)
            {
                if (_loop)
                {
                    var temp = _startPos;
                    _startPos = _endPos;
                    _endPos = temp;

                    _elapsedTime = 0f;
                    _progress01 = 0f;
                }
                else
                {
                    return false; // quit
                }
            }
            return true;
        }

        public void Apply(Node3D node)
        {

            var newOffset = Vector3.Lerp(_startPos, _endPos, _progress01);

            node.Position = newOffset;
        }

        public void Reset()
        {
            _elapsedTime = 0f;
            _progress01 = 0f;
        }
    }
}
