using Engine;

namespace Spacebox.Scenes.Test
{
    public abstract class Component
    {
        public SceneNode Owner { get; private set; }

        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public virtual void OnAttached() { }
        public virtual void OnDettached() { }
        public virtual void LoadResources() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void Render() { }
        public virtual void UnloadResources() { }

        public void SetOwner(SceneNode owner)
        {
            Owner = owner;
        }
    }
}
