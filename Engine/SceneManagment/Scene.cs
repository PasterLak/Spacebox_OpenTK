using Engine.Physics;
using System.Xml.Linq;

namespace Engine.SceneManagment
{
    public abstract class Scene : Node3D, IDisposable
    {
      
        public CollisionManager CollisionManager { get; private set; }
       
        public List<IDisposable> Disposables { get; private set; } = new List<IDisposable>();

        public Scene(string[] args)
        {

            Init();
        }

        private void Init()
        {
            CollisionManager = new CollisionManager();
            

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
         BVHCuller.Render(this, Camera.Main);
    
        }

        public abstract void OnGUI();

        public virtual void Dispose()
        {
           
               
            if (Disposables.Count > 0)
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
