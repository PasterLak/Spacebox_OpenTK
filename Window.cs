using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Spacebox.Scenes;
using System.Collections.Concurrent;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Dear_ImGui_Sample;
using System;
using Spacebox.Extensions;
using Spacebox.Entities;

namespace Spacebox
{

    public class Window : GameWindow
    {

        public static Window Instance;
        
        private SceneManager _sceneManager;
        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        public static Action<Vector2> OnResized;

        private AudioManager _audioManager;
        private bool _isFullscreen = false;
        private Vector2i _previousSize;
        private Vector2i _previousPos;
        ImGuiController _controller;
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

            

            /*if(!File.Exists(path))
            {
                NumberStorage.SaveNumbers(path, Location.X, Location.Y);
                _previousPos = Location;
            }
            else
            { 
                var(x,y) = NumberStorage.LoadNumbers(path);
                _previousPos = new Vector2i(x,y);
                Location = _previousPos;
            }*/

            _previousSize = Size;

            // _audioManager = AudioManager.Instance;

           
            Debug.Log("[Engine started!]");

            //this.VSync = VSyncMode.On;

            FrameLimiter.Initialize(120);
            FrameLimiter.IsRunning = true;

           

            _sceneManager = new SceneManager(this, typeof(LogoScene));

            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            
            //Theme.ApplyDarkTheme();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Time.StartRender();

            FrameLimiter.Update();
            Time.Update(e);
            _controller.Update(this, (float)e.Time);


            if (SceneManager.CurrentScene != null)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                SceneManager.CurrentScene.Render();

                VisualDebug.Render();
                /*GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                SceneManager.CurrentScene.OnGUI();

                GL.Disable(EnableCap.Blend);
                */
                //ImGui.ShowDemoWindow();
                Time.StartOnGUI();
                SceneManager.CurrentScene.OnGUI();

                Vector2 windowSize = new Vector2(ClientSize.X, ClientSize.Y);
                Debug.Render(windowSize.ToSystemVector2());
                Time.EndOnGUI();

                _controller.Render();

                ImGuiController.CheckGLError("End of frame");

            }

            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // Enable Docking
            // ImGui.DockSpaceOverViewport();


            Time.EndRender();

            SwapBuffers();

        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Time.StartUpdate();


            Time.Update(e);
            //_controller.Update(this, Time.Delta);
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

            if (Input.IsKeyDown(Keys.F1))
            {
                Debug.ToggleVisibility();
            }

            if (Input.IsKeyDown(Keys.F11))
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


                _controller.WindowResized(ClientSize.X, ClientSize.Y);
                _isFullscreen = !_isFullscreen;
            }


            if (Input.IsKeyDown(Keys.KeyPadAdd))
            {
                Lighting.AddAmbient();
            }

            if (Input.IsKeyDown(Keys.KeyPadSubtract))
            {
                Lighting.RemoveAmbient();
            }

            if (Input.IsKeyDown(Keys.F4))
            {
                VisualDebug.ShowDebug = !VisualDebug.ShowDebug;
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                Quit();
            }

            if (Input.IsKeyDown(Keys.F7))
            {
                if(FrameLimiter.TargetFPS == 120)
                {
                    FrameLimiter.TargetFPS = 9999;
                }
                else
                {
                    FrameLimiter.TargetFPS = 120;
                }
            }

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Update();
                SceneManager.CurrentScene.LateUpdate();
            }


            Time.EndUpdate();


        }

        private void CenterWindow()
        {
        
            var (monitorWidth, monitorHeight) = Monitors.GetMonitorFromWindow(this).WorkArea.Size;

          
            int windowWidth = Size.X;
            int windowHeight = Size.Y;

            
            int posX = (monitorWidth - windowWidth) / 2;
            int posY = (monitorHeight - windowHeight) / 2;

       
            Location = new Vector2i(posX, posY);
        }

        public void Quit()
        {
            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.UnloadContent();

            }
            AudioManager.Instance.Dispose();
            NumberStorage.SaveNumbers(path, Location.X, Location.Y);

            ShaderManager.DisposeAll();
            TextureManager.DisposeAll();

            Close();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _controller.MouseScroll(e.Offset);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);


            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            //GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            OnResized?.Invoke(Size);

            if (Camera.Main != null)
            {
                Camera.Main.AspectRatio = (float)Size.X / Size.Y;
            }
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }



    }
}
