using Engine;
using Engine.Audio;
using Engine.Graphics;
using Engine.GUI;
using Engine.InputPro;
using Engine.PostProcessing;
using Engine.SceneManagement;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.GUI;
using Spacebox.Scenes;


namespace Spacebox
{
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

#if DEBUG
            SceneManager.Load<MenuScene>();
#else
            SceneManager.Load<LogoScene>();
#endif
        }

        protected override void OnRegisterScenes()
        {
            SceneManager.Register<MenuScene>();
            SceneManager.Register<PlaygroundScene>();
            SceneManager.Register<LocalSpaceScene>();
            SceneManager.Register<MultiplayerScene>();
            SceneManager.Register<LogoScene>();
            SceneManager.Register<ParticleSystemEditor>();
        }

        protected override void OnLoadImGui()
        {
            LoadStarPixelFont();
        }

        protected override void OnSetupInput()
        {
            InputManager0.AddAction("screenshot", Keys.F12, true);
            InputManager0.RegisterCallback("screenshot", () => { _doScreenshot = true; });
        }

        protected override void OnSetupPostProcessing(PostProcessManager processManager)
        {
            var blackWhiteShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/blackWhite", true);

            var vignetteShader = Resources.Load<Shader>("Resources/Shaders/PostProcessing/vignette", true);
            var vignetteEffect = new VignetteEffect(vignetteShader);
            vignetteEffect.Enabled = false;
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
    }
}