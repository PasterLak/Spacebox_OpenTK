using Engine.Graphics.Rhi;
using System.Drawing;

namespace Engine.SceneManagement
{
    public class ErrorScene : Scene
    {
 
        public ErrorScene()
        {
        }

        public override void LoadContent()
        {
            // Window.Instance.Title = "Error";

          
        }

        public override void Start()
        {
            
        }


        public override void Render()
        {
            Gpu.Device.SetClearColor(OpenTK.Mathematics.Color4.Red);
            Gpu.Device.Clear(ClearTargets.Color | ClearTargets.Depth);
        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
        }

        public override void Update()
        {
        }
    }
}