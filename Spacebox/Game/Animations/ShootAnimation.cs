using OpenTK.Mathematics;
using Spacebox.Engine;
using Spacebox.Engine.Animation;

namespace Spacebox.Game.Animations
{
    public class ShootAnimation : IAnimation
    {
        private Vector3 _startPos;
        private Vector3 _endPos;
        private float _duration;

        private float _elapsedTime;
        private float _progress01;

        private bool _loop;

        public ShootAnimation(Vector3 startPos, Vector3 endPos, float duration)
        {
            _startPos = startPos;
            _endPos = endPos;
            _duration = duration;
          _loop = true;
        }

        public void Init()
        {
            _elapsedTime = 0f;
            _progress01 = 0f;
        }
        private byte count = 1;
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
                    if (count == 2) return false;

                    var temp = _startPos;
                    _startPos = _endPos;
                    _endPos = temp;

                    _elapsedTime = 0f;
                    _progress01 = 0f;
                    count++;
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
