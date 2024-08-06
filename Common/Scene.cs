using OpenTK.Windowing.Desktop;

namespace Spacebox.Common
{
    public abstract class Scene
    {
        protected GameWindow Content;
        public string Name { get; private set; }
        public SceneManager SceneManager { get; private set; }
        public Scene()
        {
            //SceneManager = sceneManager;
            //Name = name;

        }

        public abstract void LoadContent();

        public abstract void UnloadContent();

        public virtual void Awake() { }
        public virtual void Start() { }

        public abstract void Update();

        public virtual void LateUpdate() { }

        public abstract void Render();


    }
}
