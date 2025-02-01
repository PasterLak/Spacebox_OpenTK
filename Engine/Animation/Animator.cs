using Engine;
using Engine.Animation;

namespace Engine.Animation
{
    public class Animator
    {
        public bool IsActive { get; set; } = true;
        public float speed = 1f;
        public bool IsPlaying => IsActive;
        private List<IAnimation> _animations = new List<IAnimation>();
        private Node3D _target;

        public Animator(Node3D target)
        {
            _target = target;
        }

        public void AddAnimation(IAnimation animation)
        {
            animation.Init();
            _animations.Add(animation);
        }

        public void Update(float deltaTime)
        {
            if(!IsActive) return;   
            if (_animations.Count == 0) return;
           

            for (int i = _animations.Count - 1; i >= 0; i--)
            {
                var anim = _animations[i];
                bool isActive = anim.Update(deltaTime * speed);
                if (!isActive)
                {
                    _animations.RemoveAt(i);
                    continue;
                }
                else
                {
                    anim.Apply(_target);
                }
            }
        }

        public void Clear()
        {
            _animations.Clear();
        }
    }
}
