using Engine.Audio;
using Engine;
using Engine.Graphics;
using Engine.GUI;
using Engine.InputPro;
using Engine.Light;
using Engine.Multithreading;
using Engine.PostProcessing;
using Engine.SceneManagement;
using Engine.UI;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Concurrent;

namespace Engine
{
    public abstract class EngineWindow : GameWindow
    {
        public static EngineWindow Instance { get; private set; }
        public static Action<Vector2> OnResized;

        public static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private bool _isFullscreen = false;
        private bool _debugUI = false;
        private ImGuiController _controller;
        private PostProcessManager _processManager;
        private Vector2i _minimizedWindowSize;
        private SceneRenderer _sceneRenderer;
        private FullscreenRenderer _fullscreenRenderer;
        private InputManager _inputManager;
        private Shader _passthroughShader;

        private static Keys _debugKey;
        private static PolygonMode _polygonMode = PolygonMode.Fill;

        static readonly Prof.Token T_VisualDebug = Prof.RegisterTimer("Visual Debug");
        static readonly Prof.Token T_SceneRender = Prof.RegisterTimer("Scene Render");
        static readonly Prof.Token T_LightSystemUpdate = Prof.RegisterTimer("Render.LightSystem");
        static readonly Prof.Token T_Render = Prof.RegisterTimer("Render.All");
        static readonly Prof.Token T_OnGUI = Prof.RegisterTimer("Render.OnGUI");
        static readonly Prof.Token T_Update = Prof.RegisterTimer("Update.All");
        static readonly Prof.Token T_UpdateScene = Prof.RegisterTimer("Update.Scene");
        static readonly Prof.Token T_PostProcessing = Prof.RegisterTimer("Render.PostFX");
        static readonly Prof.Token T_Swap = Prof.RegisterTimer("Render.Swap");

        public EngineWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
            _minimizedWindowSize = nativeWindowSettings.ClientSize;

            GPUDebug.Initialize(true);

            _inputManager = InputManager.Instance;
            _inputManager.Initialize(this);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            Debug.Log("[Engine started!]");
            Input.Initialize(this);

            _sceneRenderer = new SceneRenderer(ClientSize.X, ClientSize.Y);
            FrameLimiter.Initialize(120);
            FrameLimiter.IsRunning = true;

            SceneManager.Initialize(() => OnRegisterScenes());

            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            OnLoadImGui();

            _debugKey = Application.Platform == Application.PlatformName.OSX ? (Keys)161 : Keys.GraveAccent;

            SetupDefaultInputBindings();
            OnSetupInput();

            CenterWindow();

            _processManager = new PostProcessManager();
            SetupDefaultPostProcessing();
            OnSetupPostProcessing(_processManager);

            MinimumSize = new Vector2i(640, 360);
            _fullscreenRenderer = new FullscreenRenderer();
            _passthroughShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/passthrough", true);

            OnGameLoad();
        }

        private void SetupDefaultInputBindings()
        {
            InputManager0.AddAction("overlay", Keys.F3, true);
            InputManager0.RegisterCallback("overlay", () => { Overlay.IsVisible = !Overlay.IsVisible; });

            InputManager0.AddAction("polygodMode", Keys.F10, true);
            InputManager0.RegisterCallback("polygodMode", TogglePolygonMode);

            InputManager0.AddAction("fullscreen", Keys.F11, true);
            InputManager0.RegisterCallback("fullscreen", ToggleFullScreen);

            InputManager0.AddAction("visualDebug", Keys.F4, true);
            InputManager0.RegisterCallback("visualDebug", () =>
            {
                if (!Input.IsKey(Keys.LeftAlt))
                    VisualDebug.Enabled = !VisualDebug.Enabled;
            });

            InputManager0.AddAction("frameLimiter", Keys.F7, true);
            InputManager0.RegisterCallback("frameLimiter", ToggleFrameLimiter);

            InputManager0.AddAction("debugUI", Keys.F9, true);
            InputManager0.RegisterCallback("debugUI", () => { _debugUI = !_debugUI; });
        }

        private void SetupDefaultPostProcessing()
        {
            var passthroughShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/passthrough", true);
            _processManager.AddEffect(new DefaultEffect(passthroughShader));
        }

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

                _sceneRenderer.Render(() => RenderScene());

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GLState.DepthTest(false);
                GLState.CullFace(false);

                using (GPUDebug.Group("POST PROCESSING"))
                {
                    using (Prof.Time(T_PostProcessing))
                    {
                        _processManager.Process(_sceneRenderer, ClientSize);
                    }
                }

                using (Prof.Time(T_OnGUI))
                {
                    using (GPUDebug.Group("GUI"))
                    {
                        Time.StartOnGUI();
                        RenderGUI();
                        Time.EndOnGUI();
                    }
                }

                using (Prof.Time(T_Swap))
                {
                    SwapBuffers();
                }

                Time.EndRender();
                OnPostRender();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Prof.ResetFrame();

            using (Prof.Time(T_Update))
            {
                base.OnUpdateFrame(e);
                RenderSpace.BeginFrame();
                Input.Update();
                Time.StartUpdate();
                Time.Update(e);
                _controller.Update(this, (float)e.Time);
                _inputManager.Update();
                InputManager0.Update();

                while (_mainThreadActions.TryDequeue(out var action))
                {
                    try { action.Invoke(); }
                    catch (Exception ex) { Debug.Error($"Exception in main thread action: {ex}"); }
                }

                UpdateDefaultInputs();
                OnUpdateInput();

                using (Prof.Time(T_UpdateScene))
                {
                    SceneManager.Update();
                }

                GlobalUniforms.Push();
                Time.EndUpdate();
                AudioDevice.Instance.Update();
            }
        }

        private void RenderScene()
        {
            if (SceneManager.Current != null)
            {
                using (Prof.Time(T_SceneRender))
                    SceneManager.Current.Render();

                using (Prof.Time(T_VisualDebug))
                    VisualDebug.Render();
            }

            if (FramebufferCapture.IsActive)
                FramebufferCapture.IsActive = false;
        }

        private void RenderGUI()
        {
            if (SceneManager.Current != null)
            {
                SceneManager.Current.OnGUI();
                Debug.Render(new Vector2(ClientSize.X, ClientSize.Y).ToSystemVector2());
                Overlay.OnGUI();
                DevGui.Draw();
                //InputOverlay.OnGUI();

                OnRenderGameGUI();

                if (_debugUI)
                {
                    ImGui.ShowDemoWindow();
                }
            }

            if (Camera.Main != null)
                _controller.Render();

            ImGuiController.CheckGLError("End of frame");
        }

        private void UpdateDefaultInputs()
        {
            if (Input.IsKeyDown(Keys.KeyPadAdd))
                Lighting.AddAmbient();
            if (Input.IsKeyDown(Keys.KeyPadSubtract))
                Lighting.RemoveAmbient();
            if (Input.IsKeyDown(_debugKey))
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
                ClientSize = _minimizedWindowSize;
                FrameLimiter.IsRunning = true;
                VSync = VSyncMode.Off;
            }
            _isFullscreen = !_isFullscreen;
            OnResized?.Invoke(Size);
        }

        public void TogglePolygonMode()
        {
            if (_polygonMode == PolygonMode.Fill)
                _polygonMode = PolygonMode.Line;
            else
                _polygonMode = PolygonMode.Fill;
            GL.PolygonMode(MaterialFace.FrontAndBack, _polygonMode);
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

        public virtual void Quit()
        {
            if (SceneManager.Current != null)
                SceneManager.Current.UnloadContent();

            AudioDevice.Instance.Dispose();
            _processManager.Dispose();
            Resources.UnloadAll(true);
            _sceneRenderer.Dispose();
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
            _minimizedWindowSize = ClientSize;
            _sceneRenderer.Resize(ClientSize.X, ClientSize.Y);
            _processManager.UpdateResolution(ClientSize);
            OnResized?.Invoke(Size);
            if (Camera.Main != null)
                Camera.Main.AspectRatio = (float)Size.X / Size.Y;
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected abstract void OnRegisterScenes();
        protected abstract void OnGameLoad();
        protected virtual void OnLoadImGui() { }
        protected virtual void OnSetupInput() { }
        protected virtual void OnSetupPostProcessing(PostProcessManager processManager) { }
        protected virtual void OnRenderGameGUI() { }
        protected virtual void OnUpdateInput() { }
        protected virtual void OnPostRender() { }
    }
}