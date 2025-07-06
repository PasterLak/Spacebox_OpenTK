using Engine.Physics;

namespace Engine.SceneManagment
{
    public abstract class Scene : Node3D, IDisposable
    {
      
        public CollisionManager CollisionManager { get; private set; }
        public Renderer Renderer { get; private set; }
        public List<IDisposable> Disposables { get; private set; } = new List<IDisposable>();

        public Scene(string[] args)
        {

            Init();
        }

        private void Init()
        {
            CollisionManager = new CollisionManager();
            Renderer = new Renderer();

            Name = "DefaultName";
            Resizable = false;
        }

        public abstract void LoadContent();
        public abstract void UnloadContent();

        public virtual void Start() { }

        public virtual void Update()
        {
            base.Update();
        }


        public virtual void Render()
        {
            base.Render();
        }

        public abstract void OnGUI();

        public void Dispose()
        {

            if(Disposables.Count > 0)
            {
                foreach(IDisposable disposable in Disposables)
                {
                    disposable.Dispose();
                }

                Disposables = null;
            }
            
        }

      
    }
}
