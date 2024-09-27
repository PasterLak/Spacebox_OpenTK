using System;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Spacebox.Scenes;
using System.Collections.Concurrent;

namespace Spacebox
{

    public class Window : GameWindow
    {

        public static Window Instance;
        
        private SceneManager _sceneManager;
        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        public static Action<Vector2> OnResized;
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

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Time.Update(e);

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Render();
            }

        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Time.Update(e);

            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    Console.WriteLine("Executing action from main thread actions queue.");
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in main thread action: {ex}");
                }
            }

            if (!IsFocused)
            {
               // return;
            }


          if(Input.IsKeyDown(Keys.F11))
            {
                

                ClientSize = new Vector2i(1920, 1080);
                GL.Viewport(0, 0, 1920,1080);
                //_camera.AspectRatio = 1920f / 1080f;
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                Quit();
            }

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Update();
                SceneManager.CurrentScene.LateUpdate();
            }


            //SceneManager.StartNextScene();

            return;

            
        }

        private void Quit()
        {
            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.UnloadContent();

            }
            Close();
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

            OnResized?.Invoke(Size);
            //_camera.AspectRatio = Size.X / (float)Size.Y;
        }

    }
}
