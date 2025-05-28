using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Engine.GUI;
using Engine.SceneManagment;

using Engine;
using Engine.Audio;

namespace Spacebox.Scenes
{
    public class LogoScene : Scene
    {

        Sprite sprite;
        AudioSource audio;
        public LogoScene(string[] args) : base(args)
        {
        }


        public override void LoadContent()
        {

            Resources.LoadAllAsync<AudioClip>(
                new[]
                {
                    "Resources/Audio/death2.ogg",
                 "Resources/Audio/scroll.ogg",
                  "Resources/Audio/Music/music.ogg",
                   "Resources/Audio/Music/ambientMain.ogg",
                });

            Resources.LoadAllAsync<Texture2D>(
               new[]
               {
                    "Resources/Textures/Space/planet2.png",

               });

            Resources.Load<Shader>("Shaders/sprite");
            // Resources.Load<AudioClip>("Resources/Audio/scroll.ogg");

            var texture = Resources.Get<Texture2D>("Resources/Textures/Space/planet2.png");

            var sp = Resources.Get<Shader>("sprite");

            sprite = new Sprite(texture, new Vector2(0, 0),
                new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y), sp);

            var clip = Resources.Get<AudioClip>("ambientMain");
            audio = new AudioSource(clip);

        }

        public override void Start()
        {

            SceneManager.LoadScene(typeof(MenuScene));
            //SceneManager.LoadScene(typeof(TestScene));

            Input.HideCursor(); audio.Play();

            Debug.Log(GL.GetString(StringName.Renderer));
            Debug.Log(GL.GetString(StringName.Vendor));
            Debug.Log(GL.GetString(StringName.Version));

           // Environment.SetEnvironmentVariable("CUDA_VISIBLE_DEVICES", "-1");
           // Environment.SetEnvironmentVariable("GPU_DEVICE_ORDINAL", "-1");
          //  Environment.SetEnvironmentVariable("LIBGL_ALWAYS_SOFTWARE", "1"); 


        }

        public override void Render()
        {
            //GL.Clear(ClearBufferMask.ColorBufferBit);


            sprite.Render(new Vector2(0, 0), new Vector2(1, 1));

        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
            sprite.Dispose();
            audio.Dispose();

        }

        public override void Update()
        {

            sprite.UpdateWindowSize(Window.Instance.ClientSize);
            sprite.UpdateSize(Window.Instance.Size);
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
           // SceneSwitcher.Update(typeof(AScene));
            if (Input.IsKeyDown(Keys.S))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

            if (Input.IsKeyDown(Keys.T))
            {
                SceneManager.LoadScene(typeof(PlanetsScene));
            }

            if (Input.IsKeyDown(Keys.E))
            {
                SceneManager.LoadScene(typeof(BScene));
            }

            if (Input.IsKeyDown(Keys.R))
            {
                sprite.Shader.ReloadShader();
            }

        }
    }
}
