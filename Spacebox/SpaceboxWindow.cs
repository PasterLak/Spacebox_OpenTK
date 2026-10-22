using Engine;
using Engine.Audio;
using Engine.InputPro;
using Engine.PostProcessing;
using Engine.PostProcessing.CameraFeed;
using Engine.PostProcessing.ColorGrading;
using Engine.PostProcessing.Glare;
using Engine.PostProcessing.Ssao;
using Engine.SceneManagement;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Network;
using Spacebox.Network.Dedicated;
using Spacebox.Scenes;


namespace Spacebox;

public class SpaceboxWindow : EngineWindow
{
    private bool _doScreenshot = false;
    private AudioSource _screenshotAudio;

    public SsaoEffect Ssao { get; private set; }
    public VignetteEffect Vignette { get; private set; }
    public ColorGradingEffect ColorGrading { get; private set; }
    public CameraFeedEffect CameraFeed { get; private set; }
    public LensGlareEffect LensGlare { get; private set; }

    public SpaceboxWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnGameLoad()
    {
        _screenshotAudio = new AudioSource(Resources.Load<AudioClip>("screenshot", true), AudioChannel.UI);

        ApplyDisplaySettings();
        Debug.MirrorToStandardOutput = LaunchOptions.IsHidden || LaunchOptions.IsDedicated;
        SpaceboxNetwork.Initialize();
        InputManager.Instance.LoadConfiguration("Resources/default_input.json");

        if (LaunchOptions.IsDedicated)
        {
            DedicatedServer.Launch();
            return;
        }

#if AGENT_API
        Agent.AgentHost.Start();
#endif

        if (!string.IsNullOrEmpty(LaunchOptions.EditorGameSet) && Spacebox.Editor.GameSetEditorLauncher.TryOpen(LaunchOptions.EditorGameSet, LaunchOptions.EditorPanel))
            return;

        if (LaunchOptions.UiShowcase)
        {
            SceneManager.Load<UiShowcaseScene>();
            return;
        }

        if (LaunchOptions.HasConnectTarget)
        {
            QuickJoin.Start(LaunchOptions.ConnectAddress, LaunchOptions.ConnectPort, LaunchOptions.PlayerName);
            return;
        }

#if AGENT_API
        if (Agent.AgentHost.TryLoadWorld()) return;
#endif

#if DEBUG
        SceneManager.Load<MenuScene>();
#else
        SceneManager.Load<MenuScene>();
#endif                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
    }

    private void ApplyDisplaySettings()
    {
        Settings.LoadSettings();
        SettingsApplier.ApplyAll();
        OnWindowModeChanged += SaveWindowMode;
    }

    private static void SaveWindowMode(WindowMode mode)
    {
        if (Settings.Graphics.WindowMode == mode) return;

        Settings.Graphics.WindowMode = mode;
        SettingsService.Save(Settings.AsGameSettings());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!IsQuitting && LaunchOptions.IsDedicated && Spacebox.Network.NetworkSession.Current != null)
        {
            e.Cancel = true;
            Spacebox.Network.Dedicated.ServerRestart.Stop();
            return;
        }

        if (!IsQuitting && Spacebox.Game.GUI.PauseUI.TryPromptBeforeLeaving(Quit))
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnUnload()
    {

        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        SettingsApplier.UpdateFocusMute(IsFocused);
#if AGENT_API
        Agent.AgentHost.Update();
#endif

        if (Input.IsKeyDown(Keys.F12))
        {
            _doScreenshot = true;
        }
    }
    protected override void OnRegisterScenes()
    {
        SceneManager.Register<MenuScene>();
        SceneManager.Register<ServerMessageScene>();
        SceneManager.Register<PlaygroundScene>();
        SceneManager.Register<LocalSpaceScene>();
        SceneManager.Register<Spacebox.Editor.GameSetEditorScene>();
        SceneManager.Register<LogoScene>();
        SceneManager.Register<ParticleSystemEditor>();
        SceneManager.Register<UiShowcaseScene>();
    }

    protected override void OnLoadImGui()
    {
        // LoadStarPixelFont();
    }

    protected override void OnSetupInput()
    {
       // InputManager0.AddAction("screenshot", Keys.F12, true);
        //InputManager0.RegisterCallback("screenshot", () => { _doScreenshot = true; });

        return;
        var action = InputManager.Instance.AddAction("screenshot", "");
        var binding = new KeyBinding(Keys.F12);
        action.Subscribe(InputEventType.Pressed, () => { _doScreenshot = true; });
        action.AddBinding(binding);
        
    }

    protected override void OnSetupPostProcessing(PostProcessManager processManager)
    {
        var vignetteShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/vignette", true);
        var vignetteEffect = new VignetteEffect(vignetteShader);
        vignetteEffect.Enabled = true;

        Ssao = new SsaoEffect(LoadSsaoShaders(), SceneRenderer);
        processManager.AddEffect(Ssao);
        processManager.AddEffect(vignetteEffect);
        Vignette = vignetteEffect;

        LensGlare = new LensGlareEffect(Resources.Load<Shader>("Resources/Shaders/PostProcessing/lensGlare", true));
        processManager.AddEffect(LensGlare);

        ColorGrading = new ColorGradingEffect(Resources.Load<Shader>("Resources/Shaders/PostProcessing/colorGrading", true));
        processManager.AddEffect(ColorGrading);

        CameraFeed = new CameraFeedEffect(Resources.Load<Shader>("Resources/Shaders/PostProcessing/cameraFeed", true));
        processManager.AddEffect(CameraFeed);
    }

    private static SsaoShaders LoadSsaoShaders()
    {
        return new SsaoShaders(
            Resources.Load<Shader>("Resources/Shaders/PostProcessing/ssaoDepth", true),
            Resources.Load<Shader>("Resources/Shaders/PostProcessing/ssaoOcclusion", true),
            Resources.Load<Shader>("Resources/Shaders/PostProcessing/ssaoBlur", true),
            Resources.Load<Shader>("Resources/Shaders/PostProcessing/ssaoComposite", true));
    }

    protected override void OnPostRender()
    {
        if (_doScreenshot)
        {
            Screenshot();
            _doScreenshot = false;
        }
    }

    private void Screenshot()
    {
        FramebufferCapture.SaveScreenshot(this);
        _screenshotAudio.Play();
        ColorOverlay.FadeOut(new System.Numerics.Vector3(1, 1, 1), 0.2f);
    }

    private static unsafe void LoadStarPixelFont(float fontSize = 16.0f)
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
        Instance.RebuildImGuiFontTexture(true);
    }
}