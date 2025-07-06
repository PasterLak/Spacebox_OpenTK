namespace Engine.Components
{
    public abstract class Component
    {
        public Node3D Owner { get; private set; }

        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public virtual void OnAttached() { }
        public virtual void OnDettached() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void Render() { }

        public void SetOwner(Node3D owner)
        {
            Owner = owner;
        }
    }
}
