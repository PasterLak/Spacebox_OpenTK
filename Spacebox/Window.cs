﻿
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Spacebox.Scenes;
using Engine.Audio;
using Engine.SceneManagment;

using ImGuiNET;
using Engine.Utils;
using Engine.GUI;
using Spacebox.Game.GUI;
using Engine;

namespace Spacebox
{
    public interface IGameWindow
    {
        void Quit();
    }
    public class Window : EngineWindow, IGameWindow
    {

        public static Window Instance;



        public static Action<Vector2> OnResized;


        private bool _isFullscreen = false;
        private bool _debugUI = false;

        private ImGuiController _controller;
        private string path = "Resources/WindowPosition.txt";

        private static Keys debugKey;
        private static PolygonMode polygonMode = PolygonMode.Fill;
        private Vector2i minimizedWindowSize = new Vector2i(500, 500);

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
            minimizedWindowSize = nativeWindowSettings.ClientSize;
            // UpdateFrequency = 9999;
        }

        public static unsafe void LoadStarPixelFont(float fontSize = 16.0f)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fontPath = Path.Combine(basePath, "Resources", "StarPixel.ttf");

            if (!File.Exists(fontPath))
            {
                Console.WriteLine($"Font file not found: {fontPath}");
                return;
            }

            ImGuiIOPtr io = ImGui.GetIO();

            // Создаем конфигурацию шрифта без блока using, так как ImFontConfigPtr не реализует IDisposable
            ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
            config.OversampleH = 1;   // Отключаем горизонтальный оверсемплинг
            config.OversampleV = 1;   // Отключаем вертикальный оверсемплинг
            config.PixelSnapH = true; // Привязываем к пиксельной сетке

            io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config);


            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            int fontTexture;
            GL.GenTextures(1, out fontTexture);
            GL.BindTexture(TextureTarget.Texture2D, fontTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(pixels));

            // Устанавливаем Nearest фильтрацию для пиксельного вида
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            io.Fonts.SetTexID((IntPtr)fontTexture);
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
            LoadStarPixelFont();
            //ThemeUIEngine.ApplyDarkTheme();

            if (Application.Platform == Application.PlatformName.OSX)
            {
                debugKey = (Keys)(161);
            }
            else
            {
                debugKey = Keys.GraveAccent;
            }
            // InputManager.AddAction("debug", Keys.GraveAccent, true); // 161 mac
            //InputManager.RegisterCallback("debug", () => { Debug.ToggleVisibility(); });
          
            InputManager.AddAction("overlay", Keys.F3, true);
            InputManager.RegisterCallback("overlay", () => { Overlay.IsVisible = !Overlay.IsVisible; });

            InputManager.AddAction("polygodMode", Keys.F10, true);
            InputManager.RegisterCallback("polygodMode", () => { TogglePolygonMode(); });

            InputManager.AddAction("fullscreen", Keys.F11, true);
            InputManager.RegisterCallback("fullscreen", () => { ToggleFullScreen(); });

            InputManager.AddAction("visualDebug", Keys.F4, true);
            InputManager.RegisterCallback("visualDebug", () => { VisualDebug.Enabled = !VisualDebug.Enabled; });

            //InputManager.AddAction("quit", Keys.Escape, true);
            //InputManager.RegisterCallback("quit", () => { Quit(); });

            InputManager.AddAction("frameLimiter", Keys.F7, true);
            InputManager.RegisterCallback("frameLimiter", () => { ToggleFrameLimiter(); });

            InputManager.AddAction("debugUI", Keys.F9, true);
            InputManager.RegisterCallback("debugUI", () => { _debugUI = !_debugUI; });

            InputManager.AddAction("screenshot", Keys.F12, true);
            InputManager.RegisterCallback("screenshot", () => { Screenshot(); });

            screenShotAudio = new AudioSource(SoundManager.AddPermanentClip("screenshot"));
            // ToggleFullScreen();
            
            CenterWindow();

          

            MinimumSize = new Vector2i(640, 360);
        }

        private static AudioSource screenShotAudio;
        public void Screenshot()
        {
            FramebufferCapture.SaveScreenshot(this);
            screenShotAudio.Play();

            HealthColorOverlay.SetActive(new System.Numerics.Vector3(1, 1, 1), 0.2f);
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

                if (_debugUI)
                {
                    ImGui.ShowDemoWindow();
                }


                Time.EndOnGUI();

                if (Camera.Main != null)
                    _controller.Render();

                ImGuiController.CheckGLError("End of frame");

            }

            Time.EndRender();

            if (FramebufferCapture.IsActive) FramebufferCapture.IsActive = false;
            SwapBuffers();

        }



        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Time.StartUpdate();


            Time.Update(e);
            InputManager.Update();

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

            }


            Time.EndUpdate();

            AudioManager.Instance.Update();
            
           
        }

        private void UpdateInputs()
        {

            if (Input.IsKeyDown(Keys.KeyPadAdd))
            {
                Lighting.AddAmbient();
            }

            if (Input.IsKeyDown(Keys.KeyPadSubtract))
            {
                Lighting.RemoveAmbient();
            }

            if (Input.IsKeyDown(debugKey))
            {
                Debug.ToggleVisibility();
            }

            if (Input.IsKeyDown(Keys.KeyPad2))
            {
                FramebufferCapture.IsActive = true;

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

        public Vector2 GetCenter()
        {

            int windowWidth = Size.X;
            int windowHeight = Size.Y;

            int posX = windowWidth / 2;
            int posY = windowHeight / 2;

            return new Vector2i(posX, posY);
        }
        public void Quit()
        {
            if (SceneManager.CurrentScene != null)
            {
                SceneManager.CurrentScene.UnloadContent();

            }

            SoundManager.DisposeAll();
            AudioManager.Instance.Dispose();

            NumberStorage.SaveNumbers(path, Location.X, Location.Y);

            ShaderManager.DisposeAll();
            TextureManager.DisposeAll();

            Debug.SaveMessagesToFile(true);
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
