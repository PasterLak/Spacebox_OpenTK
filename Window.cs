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
using Spacebox.GUI;
using ImGuiNET;

namespace Spacebox
{

    public class Window : GameWindow
    {

        public static Window Instance;
        
        
        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        public static Action<Vector2> OnResized;


        private bool _isFullscreen = false;
        private bool _debugUI = false;

        private ImGuiController _controller;
        private string path = "Resources/WindowPosition.txt";


        private static PolygonMode polygonMode = PolygonMode.Fill;
        private Vector2i minimizedWindowSize = new Vector2i(500, 500);

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
            minimizedWindowSize = nativeWindowSettings.ClientSize;

        }

        protected override void OnLoad()
        {
            base.OnLoad();
            AppIconLoader.LoadAndSetIcon(this, "Resources/Textures/icon.png");
            Debug.Log("[Engine started!]");
            Input.Initialize(this);

            FrameLimiter.Initialize(120);
            FrameLimiter.IsRunning = true;

            SceneManager.Initialize(this, typeof(LogoScene));

            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            
            Theme.ApplyDarkTheme();
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

                Time.StartOnGUI();
                SceneManager.CurrentScene.OnGUI();

                Vector2 windowSize = new Vector2(ClientSize.X, ClientSize.Y);
                Debug.Render(windowSize.ToSystemVector2());
                Overlay.OnGUI();
                InputOverlay.OnGUI();

                if(_debugUI)
                {
                    ImGui.ShowDemoWindow();
                }


                Time.EndOnGUI();
                

                _controller.Render();

                ImGuiController.CheckGLError("End of frame");

            }

            Time.EndRender();

            SwapBuffers();

        }

        

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Time.StartUpdate();


            Time.Update(e);
            
            while (_mainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.Error($"Exception in main thread action: {ex}");
                }
            }


            UpdateInputs();

            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.Update();
                SceneManager.CurrentScene.LateUpdate();
            }


            Time.EndUpdate();


        }

        private void UpdateInputs()
        {
            if (Input.IsKeyDown(Keys.F1))
            {
                Debug.ToggleVisibility();
            }

            if (Input.IsKeyDown(Keys.F10))
            {
                TogglePolygonMode();
            }

            if (Input.IsKeyDown(Keys.F11))
            {
                ToggleFullScreen();
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
                ToggleFrameLimiter();
            }

            if(Input.IsKeyDown(Keys.F9))
            {
                _debugUI = !_debugUI;
            }
        }

        public void ToggleFrameLimiter()
        {
            if (FrameLimiter.TargetFPS == 120)
            {
                FrameLimiter.TargetFPS = 9999;
            }
            else
            {
                FrameLimiter.TargetFPS = 120;
            }
        }

        public void ToggleFullScreen()
        {
            if (!_isFullscreen)
            {

                WindowBorder = WindowBorder.Hidden;
                WindowState = WindowState.Fullscreen;
                FrameLimiter.IsRunning = false;
                VSync = VSyncMode.On;

            }
            else
            {
                WindowBorder = WindowBorder.Resizable;
                WindowState = WindowState.Normal;
                ClientSize = minimizedWindowSize;
                FrameLimiter.IsRunning = true;
                VSync = VSyncMode.Off;
            }

            _isFullscreen = !_isFullscreen;

            OnResized?.Invoke(Size);
        }

        public void TogglePolygonMode()
        {
            if (polygonMode == PolygonMode.Fill)
            {
                polygonMode = PolygonMode.Line;
            }
            else
            {
                polygonMode = PolygonMode.Fill;
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);
        }

        public void CenterWindow()
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

        //public static 
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            
            //GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            minimizedWindowSize = ClientSize;

            OnResized?.Invoke(Size);

            if (Camera.Main != null)
            {
                Camera.Main.AspectRatio = (float)Size.X / Size.Y;
            }
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }



    }
}
