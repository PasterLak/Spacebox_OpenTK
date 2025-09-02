using Engine;
using Engine.Audio;
using Engine.Graphics;
using Engine.GUI;
using Engine.InputPro;
using Engine.Light;
using Engine.Multithreading;
using Engine.PostProcessing;
using Engine.SceneManagement;
using Engine.UI;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.FPS.Scenes;
using Spacebox.Game.GUI;
using Spacebox.Scenes;


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
        private PostProcessManager _processManager;
        private static Keys debugKey;
        private static PolygonMode polygonMode = PolygonMode.Fill;
        private Vector2i minimizedWindowSize = new Vector2i(500, 500);
        private SceneRenderer SceneRenderer;
        private bool DoScreenshot = false;

        private InputManager InputManager;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
            minimizedWindowSize = nativeWindowSettings.ClientSize;

            GPUDebug.Initialize(true);

            InputManager = InputManager.Instance;
            InputManager.Initialize(this);
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
            ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
            config.OversampleH = 1;
            config.OversampleV = 1;
            config.PixelSnapH = true;

            io.Fonts.AddFontFromFileTTF(fontPath, fontSize, config);
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            int fontTexture;
            GL.GenTextures(1, out fontTexture);
            GL.BindTexture(TextureTarget.Texture2D, fontTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(pixels));
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

            SceneRenderer = new SceneRenderer(ClientSize.X, ClientSize.Y);
            FrameLimiter.Initialize(120);
            FrameLimiter.IsRunning = true;

            SceneManager.Initialize(() =>
            {
                SceneManager.Register<Spacebox.Scenes.MenuScene>();
                SceneManager.Register<Spacebox.Scenes.PlaygroundScene>();
                SceneManager.Register<Spacebox.Scenes.LocalSpaceScene>();
                SceneManager.Register<Spacebox.Scenes.MultiplayerScene>();
                SceneManager.Register<Spacebox.Scenes.LogoScene>();
                SceneManager.Register<ParticleSystemEditor>();

            });
            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            LoadStarPixelFont();

            if (Application.Platform == Application.PlatformName.OSX)
                debugKey = (Keys)161;
            else
                debugKey = Keys.GraveAccent;

            InputManager0.AddAction("overlay", Keys.F3, true);
            InputManager0.RegisterCallback("overlay", () => { Overlay.IsVisible = !Overlay.IsVisible; });
            InputManager0.AddAction("polygodMode", Keys.F10, true);
            InputManager0.RegisterCallback("polygodMode", () => { TogglePolygonMode(); });
            InputManager0.AddAction("fullscreen", Keys.F11, true);
            InputManager0.RegisterCallback("fullscreen", () => { ToggleFullScreen(); });
            InputManager0.AddAction("visualDebug", Keys.F4, true);
            InputManager0.RegisterCallback("visualDebug", () => { VisualDebug.Enabled = !VisualDebug.Enabled; });
            InputManager0.AddAction("frameLimiter", Keys.F7, true);
            InputManager0.RegisterCallback("frameLimiter", () => { ToggleFrameLimiter(); });
            InputManager0.AddAction("debugUI", Keys.F9, true);
            InputManager0.RegisterCallback("debugUI", () => { _debugUI = !_debugUI; });
            InputManager0.AddAction("screenshot", Keys.F12, true);
            InputManager0.RegisterCallback("screenshot", () => { DoScreenshot = true; });

            screenShotAudio = new AudioSource(Resources.Load<AudioClip>("screenshot", true));
            CenterWindow();

            _processManager = new PostProcessManager();

            var sha = Resources.Load<Shader>("Shaders/PostProcessing/passthrough", true);

            _processManager.AddEffect(new DefaultEffect(sha));

            var sha2 = Resources.Load<Shader>("Shaders/PostProcessing/blackWhite", true);

            //_processManager.AddEffect(new BlackWhiteEffect(sha2));

            var sha3 = Resources.Load<Shader>("Shaders/PostProcessing/vignette", true);
            var vignetteEffect = new VignetteEffect(sha3);
            vignetteEffect.Enabled = false;
            _processManager.AddEffect(vignetteEffect);

            var sha4 = Resources.Load<Shader>("Shaders/PostProcessing/edgeDetection", true);


            var normalShader = Resources.Load<Shader>("Shaders/PostProcessing/normalView", true);
            // _processManager.AddEffect(new NormalViewEffect(normalShader, SceneRenderer));

            var depthShader = Resources.Load<Shader>("Shaders/PostProcessing/depthView", true);
            //_processManager.AddEffect(new DepthViewEffect(depthShader, SceneRenderer, 0.1f, 100f));

            // _processManager.AddEffect(new EdgeDetectionEffect(sha4));

            Texture2D ssaoNoiseTex = SsaoNoise.GenerateRotationNoise(4, 1234);

            ssaoNoiseTex.SaveToPng("ssao_noise.png");


            var ssao = Resources.Load<Shader>("Shaders/PostProcessing/ssao", true);
           // _processManager.AddEffect(new SSAOEffect(ssao, SceneRenderer, ssaoNoiseTex));


            MinimumSize = new Vector2i(640, 360);
            FullscreenRenderer = new FullscreenRenderer();
            shaderpass = sha;

            //SceneManager.Load<PlaygroundScene>();
            // SceneManager.Load<ParticleSystemEditor>();
#if DEBUG
            SceneManager.Load<MenuScene>();
#else
 
            SceneManager.Load<LogoScene>();
#endif
        }

        private static AudioSource screenShotAudio;
        public void Screenshot()
        {
            FramebufferCapture.SaveScreenshot(this);
            screenShotAudio.Play();
            HealthColorOverlay.SetActive(new System.Numerics.Vector3(1, 1, 1), 0.2f);
        }

        static readonly Prof.Token T_VisualDebug = Prof.RegisterTimer("Visual Debug");
        static readonly Prof.Token T_SceneRender = Prof.RegisterTimer("Scene Render");
        private void RenderScene()
        {

            if (SceneManager.Current != null)
            {
                using (Prof.Time(T_SceneRender))
                    SceneManager.Current.Render();

                using (Prof.Time(T_VisualDebug))
                    VisualDebug.Render();
            }

            if (FramebufferCapture.IsActive) FramebufferCapture.IsActive = false;


        }

        private void OnGUI()
        {

            if (SceneManager.Current != null)
            {

                SceneManager.Current.OnGUI();
                Debug.Render(new Vector2(ClientSize.X, ClientSize.Y).ToSystemVector2());
                Overlay.OnGUI();
                DevGui.Draw();
                InputOverlay.OnGUI();
                if (_debugUI)
                {
                    ImGui.ShowDemoWindow();
                }
            }


            if (Camera.Main != null)
                _controller.Render();

            ImGuiController.CheckGLError("End of frame");
        }
        FullscreenRenderer FullscreenRenderer;
        Shader shaderpass;

        static readonly Prof.Token T_LightSystemUpdate = Prof.RegisterTimer("Render.LightSystem");
        static readonly Prof.Token T_Render = Prof.RegisterTimer("Render.All");
        static readonly Prof.Token T_OnGUI = Prof.RegisterTimer("Render.OnGUI");
        static readonly Prof.Token T_Update = Prof.RegisterTimer("Update.All");
        static readonly Prof.Token T_UpdateScene = Prof.RegisterTimer("Update.Scene");
        static readonly Prof.Token T_PostProcessing = Prof.RegisterTimer("Render.PostFX");
        static readonly Prof.Token T_Swap = Prof.RegisterTimer("Render.Swap");


        protected override void OnRenderFrame(FrameEventArgs e)
        {

            base.OnRenderFrame(e);
            using (Prof.Time(T_Render))
            {

                Time.StartRender();


                FrameLimiter.Update();


                using (Prof.Time(T_LightSystemUpdate))
                {
                    LightSystem.Update();
                }



                SceneRenderer.Render(() =>
                {
                    RenderScene();
                });



                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);


                using (GPUDebug.Group("POST PROCESSING"))
                {
                    //FullscreenRenderer.RenderToScreen(SceneRenderer.SceneTexture, shaderpass, ClientSize);
                    using (Prof.Time(T_PostProcessing))
                    {
                        _processManager.Process(SceneRenderer, ClientSize);
                    }
                }


                using (Prof.Time(T_OnGUI))
                {
                    using (GPUDebug.Group("GUI"))
                    {
                        Time.StartOnGUI();
                        OnGUI();
                        Time.EndOnGUI();
                    }
                }

                using (Prof.Time(T_Swap))
                {
                    SwapBuffers();
                }


                Time.EndRender();
            }
            if (DoScreenshot)
            {
                Screenshot();
                DoScreenshot = false;
            }


            if (ProfDumpTimer.TimeSinceLastDump() >= 1.0)
            {
                Prof.DumpToConsole();
                ProfDumpTimer.ResetDumpTimer();
            }

        }
        static class ProfDumpTimer
        {
            static readonly System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            static long lastTicks;

            public static double TimeSinceLastDump() =>
                (sw.ElapsedTicks - lastTicks) / (double)System.Diagnostics.Stopwatch.Frequency;

            public static void ResetDumpTimer() =>
                lastTicks = sw.ElapsedTicks;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Prof.ResetFrame();

            using (Prof.Time(T_Update))
            {

                base.OnUpdateFrame(e);
                RenderSpace.BeginFrame();
               
                Time.StartUpdate();
                Time.Update(e);
                _controller.Update(this, (float)e.Time);
                InputManager.Update();
                InputManager0.Update();

                while (_mainThreadActions.TryDequeue(out var action))
                {
                    try { action.Invoke(); }
                    catch (Exception ex) { Debug.Error($"Exception in main thread action: {ex}"); }
                }

                UpdateInputs();
                using (Prof.Time(T_UpdateScene))
                {

                    SceneManager.Update();
                }
                GlobalUniforms.Push();

                Time.EndUpdate();
                AudioDevice.Instance.Update();
            }
        }

        private void UpdateInputs()
        {
            if (Input.IsKeyDown(Keys.KeyPadAdd))
                Lighting.AddAmbient();
            if (Input.IsKeyDown(Keys.KeyPadSubtract))
                Lighting.RemoveAmbient();
            if (Input.IsKeyDown(debugKey))
                Debug.ToggleVisibility();
            if (Input.IsKeyDown(Keys.KeyPad2))
                FramebufferCapture.IsActive = true;
        }

        public void ToggleFrameLimiter()
        {
            if (FrameLimiter.TargetFPS == 120)
                FrameLimiter.TargetFPS = 9999;
            else
                FrameLimiter.TargetFPS = 120;
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
                polygonMode = PolygonMode.Line;
            else
                polygonMode = PolygonMode.Fill;
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
            int windowWidth = ClientSize.X;
            int windowHeight = ClientSize.Y;
            int posX = windowWidth / 2;
            int posY = windowHeight / 2;
            return new Vector2i(posX, posY);
        }

        public float GetAspectRatio()
        {

            return ClientSize.X / (float)ClientSize.Y;
        }

        public void Quit()
        {
            if (SceneManager.Current != null)
                SceneManager.Current.UnloadContent();

            AudioDevice.Instance.Dispose();
            _processManager.Dispose();
            NumberStorage.SaveNumbers(path, Location.X, Location.Y);

            Resources.UnloadAll(true);
            SceneRenderer.Dispose();
            Debug.SaveMessagesToFile(true);
            Close();

            WorkerPoolManager.Shutdown();
        }

        public static void Shutdown(object sender, EventArgs e)
        {
            Debug.SaveMessagesToFile(true);
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
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            minimizedWindowSize = ClientSize;
            SceneRenderer.Resize(ClientSize.X, ClientSize.Y);
            _processManager.UpdateResolution(ClientSize);
            OnResized?.Invoke(Size);
            if (Camera.Main != null)
                Camera.Main.AspectRatio = (float)Size.X / Size.Y;
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }
    }
}
