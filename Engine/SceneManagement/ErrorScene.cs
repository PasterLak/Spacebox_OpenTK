using OpenTK.Graphics.OpenGL4;
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
            Console.Error.WriteLine("Error Scene!");
        }


        public override void Render()
        {
            GL.ClearColor(Color.Red);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
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