using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox_OpenTK.Common;
using System.Drawing;

namespace Spacebox_OpenTK.Scenes
{
    internal class ErrorScene : Scene
    {
        public ErrorScene()
        {
        }


        public override void LoadContent()
        {
           

            GL.ClearColor(Color.Red);

            //GL.Enable(EnableCap.DepthTest);

        }

        public override void Awake()
        {
           
        }
        public override void Start()
        {
          
        }


        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            SceneManager.Instance.GameWindow.SwapBuffers();
        }

        public override void UnloadContent()
        {
           
        }

        public override void Update()
        {
          
        }
    }
}
