using System;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Spacebox.Scenes;
using System.Collections.Concurrent;
using System.IO;

namespace Spacebox
{

    public class Window : GameWindow
    {

        public static Window Instance;
        
        private SceneManager _sceneManager;
        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        public static Action<Vector2> OnResized;


        private bool _isFullscreen = false;
        private Vector2i _previousSize;
        private Vector2i _previousPos;

        string path = "Resources/WindowPosition.txt";

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Input.Initialize(this);

            

            if(!File.Exists(path))
            {
                NumberStorage.SaveNumbers(path, Location.X, Location.Y);
                _previousPos = Location;
            }
            else
            { 
                var(x,y) = NumberStorage.LoadNumbers(path);
                _previousPos = new Vector2i(x,y);
                Location = _previousPos;
            }

            _previousSize = Size;
            
            Console.WriteLine("Engine started!");

            //this.VSync = VSyncMode.On;

            FrameLimiter.Initialize(60);
            FrameLimiter.IsRunning = true;

            _sceneManager = new SceneManager(this, typeof(LogoScene));

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            FrameLimiter.Update();
            Time.Update(e);
           
            //Debug.Render();

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
            //Console.WriteLine("FPS: " + Time.FPS);

          if(Input.IsKeyDown(Keys.F11))
            {
                var size = Monitors.GetMonitorFromWindow(this).ClientArea.Size;

                if (_isFullscreen)
                {
                 
                    size = _previousSize;
               

                }
                

                ClientSize = size;
                GL.Viewport(0, 0, size.X,size.Y);
                //_camera.AspectRatio = 1920f / 1080f;
               
                if(!_isFullscreen)
                {
                    //WindowState = WindowState.Fullscreen;
                    WindowBorder = WindowBorder.Hidden;
                    CenterWindow();
                }else
                {
                    WindowBorder = WindowBorder.Resizable;
                    Location = _previousPos;
                   // WindowState = WindowState.Normal;
                }


                _isFullscreen = !_isFullscreen;
            }

            if (Input.IsKeyDown(Keys.F4))
            {
                Debug.ShowDebug = !Debug.ShowDebug;
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

        private void CenterWindow()
        {
            // Получаем разрешение монитора
            var (monitorWidth, monitorHeight) = Monitors.GetMonitorFromWindow(this).WorkArea.Size;

            // Получаем размер окна
            int windowWidth = Size.X;
            int windowHeight = Size.Y;

            // Вычисляем позицию для центрирования
            int posX = (monitorWidth - windowWidth) / 2;
            int posY = (monitorHeight - windowHeight) / 2;

            // Устанавливаем позицию окна
            Location = new Vector2i(posX, posY);
        }

        private void Quit()
        {
            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.UnloadContent();

            }
            NumberStorage.SaveNumbers(path, Location.X, Location.Y);
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
