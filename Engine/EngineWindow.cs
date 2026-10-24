using Engine.Audio;
using Engine;
using Engine.Graphics;
using Engine.Graphics.Transparency;
using Engine.GUI;
using Engine.InputPro;
using Engine.Light;
using Engine.Multithreading;
using Engine.PostProcessing;
using Engine.Graphics.Debugging;
using Engine.SceneManagement;
using Engine.UI;
using Engine.Graphics.Rhi;
using Engine.Graphics.Rhi.OpenGL;
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

        private PostProcessManager _processManager;
        private Vector2i _minimizedWindowSize; 
        private SceneRenderer _sceneRenderer;
        private InputManager _inputManager;
     
        private static Keys _debugKey;
        private GBufferDebugPass _gbufferDebug;
        public Engine.Graphics.Occlusion.OcclusionCuller Occlusion { get; } = new Engine.Graphics.Occlusion.OcclusionCuller();

        static readonly Prof.Token T_VisualDebug = Prof.RegisterTimer("Visual Debug");
        static readonly Prof.Token T_SceneRender = Prof.RegisterTimer("Scene Render");
        static readonly Prof.Token T_Transparent = Prof.RegisterTimer("Render.Transparent");
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
            if (Context == null && Graphics.Headless.NoOpGraphicsBindings.IsSupported) Graphics.Headless.NoOpGraphicsBindings.Install();

            if (!Application.IsHeadless)
            {
                Gpu.Install(new OpenGlDevice());
                GPUDebug.Initialize(true);
            }

            _inputManager = InputManager.Instance;
            _inputManager.Initialize(this);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            Debug.Log("[Engine started!]");
            Input.Initialize(this);
            ResourceFallbacks.Initialize();
            if (!Application.IsHeadless)
            {
                LightSystem.InitializeGpu();
                VisualDebug.InitializeGpu();
                WorldLines.InitializeGpu();
                Gpu.Device.ResetState();
                _sceneRenderer = new SceneRenderer(SceneSize.X, SceneSize.Y);
            }

            LogSystemReport();
            FrameLimiter.Initialize(120);
            FrameLimiter.IsRunning = true;

            SceneManager.Initialize(() => OnRegisterScenes());

            _debugKey = Application.Platform == Application.PlatformName.OSX ? (Keys)161 : Keys.GraveAccent;

            SetupDefaultInputBindings();
            OnSetupInput();

            CenterWindow();

            if (!Application.IsHeadless)
            {
                _processManager = new PostProcessManager();
                SetupDefaultPostProcessing();
                OnSetupPostProcessing(_processManager);
            }

            MinimumSize = new Vector2i(640, 360);
          
            OnGameLoad();
        }

        private void SetupDefaultInputBindings()
        {
            InputManager0.AddAction("overlay", Keys.F3, true);
            InputManager0.RegisterCallback("overlay", () => { Overlay.IsVisible = !Overlay.IsVisible; });

            InputManager0.AddAction("renderDebugView", Keys.F10, true);
            InputManager0.RegisterCallback("renderDebugView", CycleRenderDebugView);

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
        }

        private void LogSystemReport()
        {
            bool gpu = !Application.IsHeadless;
            var monitor = !gpu ? null : CanPositionWindow ? Monitors.GetMonitorFromWindow(this) : Monitors.GetPrimaryMonitor();
            var report = new Diagnostics.SystemReport
            {
                GameVersion = Application.Version,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                Cpu = Diagnostics.CpuName.Detect(),
                LogicalCores = Environment.ProcessorCount,
                MemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
                GpuRenderer = gpu ? Gpu.Device.Info.Renderer : "",
                GpuVendor = gpu ? Gpu.Device.Info.Vendor : "",
                OpenGlVersion = gpu ? Gpu.Device.Info.Version : "",
                GlslVersion = gpu ? Gpu.Device.Info.ShadingLanguageVersion : "",
                Monitor = monitor == null ? "" : $"{monitor.HorizontalResolution}x{monitor.VerticalResolution}"
            };
            foreach (var line in report.Lines()) Debug.Log(line);
        }

        internal const string PassthroughShaderCode = "--Vert\r\n\r\n#version 330 core\r\nlayout (location = 0) in vec2 aPos;\r\nlayout (location = 1) in vec2 aTexCoords;\r\nout vec2 TexCoords;\r\nvoid main()\r\n{\r\n    TexCoords = aTexCoords;\r\n    gl_Position = vec4(aPos, 0.0, 1.0);\r\n}\r\n\r\n\r\n--Frag\r\n\r\n#version 330 core\r\nout vec4 FragColor;\r\nin vec2 TexCoords;\r\nuniform sampler2D scene;\r\nvoid main()\r\n{\r\n    FragColor = texture(scene, TexCoords);\r\n   \r\n}\r\n";

        private void SetupDefaultPostProcessing()
        {
           // var passthroughShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/passthrough", true);

            var code = PassthroughShaderCode;

            var passthroughShader = new Shader(code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
            Resources.AddResource("passthrough_shader", passthroughShader, true);
            _processManager.PresentShader = passthroughShader;
            _processManager.AddEffect(new DefaultEffect(passthroughShader));
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (IsQuitting) return;
            base.OnRenderFrame(e);

            if (Application.IsHeadless)
            {
                FrameLimiter.Update();
                return;
            }

            using (Prof.Time(T_Render))
            {
                Time.StartRender();
                FrameLimiter.Update();

                RenderWorld();

                using (Prof.Time(T_OnGUI))
                {
                    using (GPUDebug.Group("GUI"))
                    {
                        Time.StartOnGUI();
                        RenderGUI();
                        Time.EndOnGUI();
                    }
                }

                MainThread.RunPendingAfterRender();

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
            if (IsQuitting) return;
            Prof.ResetFrame();

            using (Prof.Time(T_Update))
            {
                SceneManager.ApplyPendingSceneChange();

                base.OnUpdateFrame(e);
                RenderSpace.BeginFrame();
                Automation.InputInjector.Apply(this);
                Input.Update();
                Time.StartUpdate();
                Time.Update(e);
                _inputManager.Update();
                InputManager0.Update();

                while (_mainThreadActions.TryDequeue(out var action))
                {
                    try { action.Invoke(); }
                    catch (Exception ex) { Debug.Error($"Exception in main thread action: {ex}"); }
                    if (IsQuitting) return;
                }
                DisposalManager.ProcessDisposals();

                UpdateDefaultInputs();
                OnUpdateInput();
                Networking.NetworkManager.Instance.Update();

                using (Prof.Time(T_UpdateScene))
                {
                    SceneManager.Update();
                }

                if (!Application.IsHeadless) GlobalUniforms.Push();
                Time.EndUpdate();
                AudioDevice.Instance.Update();
            }
        }

        public void RenderWorldFrame()
        {
            if (Application.IsHeadless || IsQuitting) return;
            RenderWorld();
        }

        private void RenderWorld()
        {
            using (Prof.Time(T_LightSystemUpdate))
            {
                LightSystem.Update();
            }

            ViewModelProjection.Reset();
            _sceneRenderer.Render(() => RenderScene());

            var device = Gpu.Device;
            device.BindFramebuffer(FramebufferHandle.Default);
            device.SetViewport(0, 0, ClientSize.X, ClientSize.Y);
            device.Clear(ClearTargets.Color | ClearTargets.Depth);

            GLState.DepthTest(false);
            GLState.CullFace(false);

            using (GPUDebug.Group("POST PROCESSING"))
            {
                using (Prof.Time(T_PostProcessing))
                {
                    _processManager.Process(_sceneRenderer, ClientSize);
                    if (RenderDebugView.ShowsBuffer)
                    {
                        _gbufferDebug ??= new GBufferDebugPass();
                        _gbufferDebug.Draw(_sceneRenderer, ClientSize, RenderDebugView.Mode);
                    }
                }
            }
        }

        private void RenderScene()
        {
            if (SceneManager.Current != null)
            {
                using (Prof.Time(T_SceneRender))
                {
                    TransparentQueue.BeginScope();
                    if (RenderDebugView.IsWireframe) Gpu.Device.SetFillMode(FillMode.Wireframe);
                    Occlusion.BeginFrame(Camera.Main);
                    try
                    {
                        SceneManager.Render();
                        Occlusion.Flush();
                    }
                    finally
                    {
                        using (Prof.Time(T_Transparent))
                            TransparentQueue.EndScope();
                        if (RenderDebugView.IsWireframe) Gpu.Device.SetFillMode(FillMode.Solid);
                    }
                }

                WorldLines.Render();

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
                WorldDebugView.DrawWorld();
                SceneManager.Current.OnGUI();
                DevConsole.Update();
                Overlay.Update();
                DevPanel.Update();
                //InputOverlay.OnGUI();

                OnRenderGameGUI();
            }

            WorldDebugView.DrawLabels();
            Overlay.Draw();
            DevPanel.Draw();
            DevConsole.Draw();
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
       
        public enum WindowMode { Fullscreen, Borderless, Windowed }

        private Vector2i _savedWindowSize;
        private Vector2i _savedWindowLocation;

        public WindowMode CurrentWindowMode { get; private set; } = WindowMode.Windowed;
        public static Action<WindowMode> OnWindowModeChanged;

        private float _renderScale = 1f;

        public float RenderScale
        {
            get => _renderScale;
            set
            {
                float clamped = Math.Clamp(value, 0.1f, 1f);
                if (clamped == _renderScale) return;
                _renderScale = clamped;
                _sceneRenderer?.Resize(SceneSize.X, SceneSize.Y);
            }
        }

        public bool PostProcessingEnabled
        {
            get => _processManager == null || _processManager.Enabled;
            set
            {
                if (_processManager != null) _processManager.Enabled = value;
            }
        }

        protected SceneRenderer SceneRenderer => _sceneRenderer;

        private Vector2i SceneSize => new Vector2i(Math.Max(1, (int)(ClientSize.X * _renderScale)), Math.Max(1, (int)(ClientSize.Y * _renderScale)));

        public void SetVSync(bool enabled)
        {
            if (Context == null) return;
            VSync = enabled ? VSyncMode.On : VSyncMode.Off;
            FrameLimiter.IsRunning = !enabled;
        }

        public void SetWindowMode(WindowMode mode)
        {
            if (mode == CurrentWindowMode) return;

            if (CurrentWindowMode == WindowMode.Windowed)
            {
                _savedWindowSize = ClientSize;
                if (CanPositionWindow) _savedWindowLocation = Location;
            }

            switch (mode)
            {
                case WindowMode.Fullscreen:
                    WindowBorder = WindowBorder.Hidden;
                    WindowState = WindowState.Fullscreen;
                    break;

                case WindowMode.Borderless:
                    WindowBorder = WindowBorder.Hidden;
                    WindowState = WindowState.Normal;

                    if (!CanPositionWindow)
                    {
                        WindowState = WindowState.Maximized;
                        break;
                    }

                    var monitor = Monitors.GetMonitorFromWindow(this);
                    Location = monitor.ClientArea.Min;
                    ClientSize = monitor.ClientArea.Size;
                    break;

                case WindowMode.Windowed:
                    WindowState = WindowState.Normal;
                    WindowBorder = WindowBorder.Resizable;

                    if (_savedWindowSize != Vector2i.Zero)
                    {
                        ClientSize = _savedWindowSize;
                        if (CanPositionWindow) Location = _savedWindowLocation;
                    }
                    else
                    {
                        CenterWindow();
                    }
                    break;
            }

            CurrentWindowMode = mode;
            OnResized?.Invoke(Size);
            OnWindowModeChanged?.Invoke(mode);
        }
        public void ToggleFullScreen()
        {
            if (CurrentWindowMode == WindowMode.Windowed)
            {
                SetWindowMode(WindowMode.Fullscreen);
            }
            else
            {
                SetWindowMode(WindowMode.Windowed);
            }
  
        }

        public void CycleRenderDebugView()
        {
            if (Input.IsKey(Keys.LeftShift) || Input.IsKey(Keys.RightShift))
                RenderDebugView.Previous();
            else
                RenderDebugView.Next();
        }

        public static bool CanPositionWindow => Platform.WindowSystemRules.SupportsWindowPosition(Platform.WindowSystemBootstrap.Current);

        public void CenterWindow()
        {
            if (!CanPositionWindow) return;
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
            return ClientSize.Y > 0 ? ClientSize.X / (float)ClientSize.Y : 1f;
        }

        public bool IsQuitting { get; private set; }

        public virtual void Quit()
        {
            if (IsQuitting) return;
            IsQuitting = true;

            if (SceneManager.Current != null)
            {
                SceneManager.Current.Destroy();
                SceneManager.Current.UnloadContent();
            }

            AudioDevice.Instance.Dispose();
            _processManager?.Dispose();
            _gbufferDebug?.Dispose();
            Occlusion.Dispose();
            ParticleSystem.DestroyAll();
            Resources.UnloadAll(true);
            ResourceScopes.Shutdown();
            DisposalManager.ProcessDisposals();
            _sceneRenderer?.Dispose();
            Debug.SaveMessagesToFile(true);
            Close();
            Overlay.Shutdown();
            WorldDebugView.Shutdown();
            DevPanel.Shutdown();
            DevConsole.Shutdown();
            WorkerPoolManager.Shutdown();
            ServiceLocator.Reset();
        }

        public static void Shutdown(object sender, EventArgs e)
        {
            Debug.SaveMessagesToFile(true);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            Input.Text.AddText(e.AsString);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            Input.Text.AddKey(new KeyPress(e.Key, e.Shift, e.Control, e.Alt, e.IsRepeat));
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            if (!Application.IsHeadless) Gpu.Device.SetViewport(0, 0, ClientSize.X, ClientSize.Y);
            _minimizedWindowSize = ClientSize;
            _sceneRenderer?.Resize(SceneSize.X, SceneSize.Y);
            _processManager?.UpdateResolution(ClientSize);
            OnResized?.Invoke(Size);
            if (Camera.Main != null && Size.X > 0 && Size.Y > 0)
                Camera.Main.AspectRatio = (float)Size.X / Size.Y;
        }

        protected abstract void OnRegisterScenes();
        protected abstract void OnGameLoad();
        protected virtual void OnSetupInput() { }
        protected virtual void OnSetupPostProcessing(PostProcessManager processManager) { }
        protected virtual void OnRenderGameGUI() { }
        protected virtual void OnUpdateInput() { }
        protected virtual void OnPostRender() { }
    }
}