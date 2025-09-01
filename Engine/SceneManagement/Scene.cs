using Engine.Light;
using Engine.Physics;

namespace Engine.SceneManagement
{
    public abstract class Scene : Node3D
    {

      
        protected Scene()
        {

            Init();
        }

        private void Init()
        {
           
            Name = "DefaultName";
            Resizable = false;
        }

        public abstract void LoadContent();
        public abstract void UnloadContent();

        public override void Start() {
            base.Start();
        }

        public override void Update()
        {
            if(Camera.Main != null)
            {
              //  Camera.Main.Update();
            }
            base.Update();
        }


        static readonly Prof.Token T_SceneSkybox = Prof.RegisterTimer("Render.Scene.Skybox");
        static readonly Prof.Token T_SceneBVH = Prof.RegisterTimer("Render.Scene.BVH");

        public virtual void Render()
        {
            if (Lighting.Skybox != null)
            {
                using(Prof.Time(T_SceneSkybox))
                Lighting.Skybox.Render();
            }
          //  base.Render();
            using (Prof.Time(T_SceneBVH))
                BVHCuller.Render(this, Camera.Main);
    
        }

        public abstract void OnGUI();

        public override void Destroy()
        {
            base.Destroy();

            if(Lighting.Skybox != null )
            {
                Lighting.Skybox.Destroy();
                Lighting.Skybox = null;
            }
           
        }
      
    }
}
