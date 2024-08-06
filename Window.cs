using System;
using Spacebox_OpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Spacebox_OpenTK.Scenes;

namespace Spacebox_OpenTK
{
    // In this tutorial we focus on how to set up a scene with multiple lights, both of different types but also
    // with several point lights
    public class Window : GameWindow
    {

        public static Window Instance;
        
        private SceneManager _sceneManager;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
            
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Input.Initialize(this);
            Console.WriteLine("Engine started!");
            _sceneManager = new SceneManager(this, typeof(LogoScene));

            //SceneManager.LoadScene(typeof(LogoScene));


            return;
                       
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Time.Update(e);

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Render();
            }

            return;

            
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Time.Update(e);

            if (!IsFocused)
            {
                return;
            }

          if(Input.IsKeyDown(Keys.F11))
            {
                

                ClientSize = new Vector2i(1920, 1080);
                GL.Viewport(0, 0, 1920,1080);
                //_camera.AspectRatio = 1920f / 1080f;
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                if(SceneManager.CurrentScene != null)
                {
                    SceneManager.CurrentScene.UnloadContent();
                    
                }
                Close();
            }

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Update();
                SceneManager.CurrentScene.LateUpdate();
            }

            //SceneManager.StartNextScene();

            return;

            
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

           // _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            //_camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
