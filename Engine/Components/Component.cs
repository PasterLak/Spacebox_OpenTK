namespace Engine.Components
{
    public abstract class Component
    {
        public Node3D Owner { get; private set; }

        public bool Enabled { get; set; } = true;

        public virtual void OnAttached(Node3D onOwner) { Owner = onOwner; }

        public virtual void OnDetached()
        {
            Owner = null;
        }
        public virtual void Start() { }
        public virtual void OnUpdate() { }
        public virtual void OnRender() { }
        public virtual void OnGUI() { }

    }
}
