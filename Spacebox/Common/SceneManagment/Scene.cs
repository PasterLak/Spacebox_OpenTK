using Spacebox.Common.Physics;

namespace Spacebox.Common.SceneManagment
{
    public abstract class Scene : Node3D, IDisposable
    {
        public SceneGraph SceneGraph { get; private set; }
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
            SceneGraph = new SceneGraph();

            Name = "DefaultName";
            Resizable = false;
        }

        public abstract void LoadContent();
        public abstract void UnloadContent();

        public virtual void Start() { }

        public abstract void Update();

        public virtual void LateUpdate() { }

        public abstract void Render();

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
