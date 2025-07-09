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
            GL.ClearColor(Color.Red);
        }

        public override void Start()
        {
            Console.Error.WriteLine("Error Scene!");
        }


        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
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