using Engine;
using Engine.Audio;
using Engine.InputPro;
using Engine.PostProcessing;
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

public interface IGameWindow
{
    void Quit();
}

public class SpaceboxWindow : EngineWindow, IGameWindow
{
    private bool _doScreenshot = false;
    private AudioSource _screenshotAudio;

    public SpaceboxWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnGameLoad()
    {
        AppIconLoader.LoadAndSetIcon(this, "Resources/Textures/icon.png");

        _screenshotAudio = new AudioSource(Resources.Load<AudioClip>("screenshot", true));

        ApplyDisplaySettings();
        Debug.MirrorToStandardOutput = LaunchOptions.IsHidden || LaunchOptions.IsDedicated;
        SpaceboxNetwork.Initialize();
        InputManager.Instance.LoadConfiguration("Resources/default_input.json");

        if (LaunchOptions.IsDedicated)
        {
            DedicatedServer.Launch();
            return;
        }

        if (!string.IsNullOrEmpty(LaunchOptions.EditorGameSet) && Spacebox.Editor.GameSetEditorLauncher.TryOpen(LaunchOptions.EditorGameSet, LaunchOptions.EditorPanel))
            return;

        if (LaunchOptions.HasConnectTarget)
        {
            QuickJoin.Start(LaunchOptions.ConnectAddress, LaunchOptions.ConnectPort, LaunchOptions.PlayerName);
            return;
        }

#if DEBUG
        SceneManager.Load<MenuScene>();
#else
        SceneManager.Load<MenuScene>();
#endif                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
    }

    private void ApplyDisplaySettings()
    {
        Settings.LoadSettings();
        SetVSync(Settings.Graphics.VSync);
        SetWindowMode(Settings.Graphics.WindowMode);
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

    protected override void OnUnload()
    {

        base.OnUnload();
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

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
        var blackWhiteShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/blackWhite", true);

        var vignetteShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/vignette", true);
        var vignetteEffect = new VignetteEffect(vignetteShader);
        vignetteEffect.Enabled = true;
        processManager.AddEffect(vignetteEffect);

        var edgeDetectionShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/edgeDetection", true);

        var normalShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/normalView", true);
        var depthShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/depthView", true);

        Texture2D ssaoNoiseTex = SsaoNoise.GenerateRotationNoise(4, 1234);
        var ssaoShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/ssao", true);
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