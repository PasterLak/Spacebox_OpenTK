using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;

namespace Spacebox.Scenes
{
    internal class MenuScene : Scene
    {

        AudioSource audio;
        AudioSource audio2;
        AudioManager audioManager;

        BitmapFont font;

        TextRenderer textRenderer;
        public MenuScene()
        {
        }

         ~MenuScene()
        {
            audio.Dispose();
            audio2.Dispose();
        }


        public override void LoadContent()
        {

            GL.ClearColor(0.8f, 0.5f, 0.3f, 1.0f);
            audioManager = new AudioManager();
            audio = new AudioSource("Resources/Audio/music.wav", audioManager.Device, audioManager.Context);
            audio.IsLooped = true;
            audio2 = new AudioSource("Resources/Audio/shooting.wav", audioManager.Device, audioManager.Context);

            font = new BitmapFont("Resources/Font/arial.png", 256,256,16,16);
            font.Spacing = 10;

          
            textRenderer = new TextRenderer(font, Window.Instance.Size.X, Window.Instance.Size.Y);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
        }
        string characters;
        public override void Awake()
        {
            audio.Play();
        }
        public override void Start()
        {
             characters =
                "A\nBCDEFGHIJKLMNOPQRSTUVWXYZ" + // 26 uppercase
                "abcdefghijklmnopqrstuvwxyz" + // 26 lowercase Aagtz
                "0123456789" +                 // 10 digits
                "!@#$%^&*()-_=+[]{}|;:',.<>/?`~\"\\ " + // 32 symbols
                "¡¢£¤¥¦§¨©ª«¬®¯°±²³´µ¶·¸¹º»¼½¾¿" + // Additional symbols
                "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞß" + // Extended Latin
                "àáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ" +
                "ĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚě" +
                "ĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĴĵĶķĹĺ" +
                "ĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕ" +
                "ŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲų" +
                "ŴŵŶŷŸŹźŻżŽž";
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);

            
           
        }

        public override void OnGUI()
        {
            //textRenderer.RenderText("FPS      " + Time.FPS.ToString(), 50f, 50f, 1f, new Vector3(0,0,0));
            textRenderer.RenderText("FPS: " + Time.FPS, 50f, 50f, 3f, new Vector3(0, 0, 0));

            textRenderer.RenderText("Spacebox\nGame\nversion 0.2", 300, 300, 3f, new Vector3(0, 0.4f, 0));

        }

        public override void UnloadContent()
        {
            audio.Dispose();
            audio2.Dispose();
            audioManager.Dispose();

            textRenderer.Dispose();
            font.Dispose();
            
        }

        public override void Update()
        {
          if(Input.IsKeyDown(Keys.Enter))
            {
                SceneManager.LoadScene(typeof(GameScene));
            }
           

            if (Input.IsKeyDown(Keys.T))
            {
                audio2.Play();
            }
        }
    }
}
