using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagment;

using Engine;
using Engine.Audio;

namespace Spacebox.Scenes
{
    public class AScene : Scene
    {

        public AScene(string[] args) : base(args)
        {
        }


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

            SceneSwitcher.Update(typeof(BScene));

            if (Input.IsKeyDown(Keys.S))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.T))
            {
                SceneManager.LoadScene(typeof(TestScene));
            }

            if (Input.IsKeyDown(Keys.B))
            {
                SceneManager.LoadScene(typeof(BScene));
            }

          

        }
    }
}
