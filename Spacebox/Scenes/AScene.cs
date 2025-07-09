using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagement;

using Engine;
using Engine.Audio;

namespace Spacebox.Scenes
{
    public class AScene : Scene
    {

      

        public override void LoadContent()
        {

         

        }

        public override void Start()
        {

        }

        public override void Render()
        {
            //GL.Clear(ClearBufferMask.ColorBufferBit);



        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
         

        }

        public override void Update()
        {

            SceneSwitcher.Update(typeof(MenuScene));

          

        }
    }
}
