using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Spacebox.Entities;
using Spacebox.Game;
using Spacebox.GUI;


namespace Spacebox.Scenes
{
    internal class SpaceMenuScene : Scene
    {

        Skybox skybox;
       
        private Shader skyboxShader;
        private Astronaut player;

        private DustSpawner spawner;

        AudioSource music;
        public SpaceMenuScene()
        {
        }


        public override void LoadContent()
        {


            // GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.ClearColor(0, 0, 0, 0);
            
            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;

            
            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            player = new Astronaut(new Vector3(0,0,0), 16 / 9f);

            skyboxShader = ShaderManager.GetShader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader,
                new SpaceTexture(512, 512));
            skybox.IsAmbientAffected = false;


            CenteredImage.LoadImage("Resources/Textures/spaceboxLogo.png", true);


            SetDustSpawner();

            music = new AudioSource(SoundManager.GetClip("music"));
            music.IsLooped = true;
            music.Play();
        }

        private void SetDustSpawner()
        {
            spawner = new DustSpawner(player);


            spawner.ParticleSystem.MaxParticles = 1000;
            spawner.ParticleSystem.SpawnRate = 200;
            spawner.Position = new Vector3(0, 0, 10);



            var emitter = new Emitter(spawner.ParticleSystem)
            {
                SpeedMin = 5f,
                SpeedMax = 10f,
                LifetimeMin = 5f,
                LifetimeMax = 10f,
                SizeMin = 0.1f,
                SizeMax = 0.2f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(1f, 1f, 1f, 0f),
                EndColorMax = new Vector4(0.9f, 0.9f, 0.9f, 0f),
                SpawnRadius = 70f,
                EmitterDirection = new Vector3(0, 0, 1),

            };

            spawner.ParticleSystem.Emitter = emitter;
        }

        public override void Awake()
        {

        }
        public override void Start()
        {
            Input.ShowCursor();
        }
        float x = 0;

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);


            skybox.DrawTransparent(player);
            spawner.Render();
            //x += 0.01f;
            //sprite.Render(new Vector2(x, 0), new Vector2(1, 1));


        }

        public override void OnGUI()
        {
            CenteredImage.Draw();
        }

        public override void UnloadContent()
        {
          

            skybox.Texture.Dispose();

            skyboxShader.Dispose();
            spawner.Dispose();
            music.Dispose();
        }

        public override void Update()
        {

            
            CenteredImage.Update();
            spawner.Update();
            //sprite.UpdateWindowSize(Window.Instance.Size);
           
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));

            if (Input.IsKeyDown(Keys.Enter))
            {
                SceneManager.LoadScene(typeof(SpaceScene));
            }

            if (Input.IsKeyDown(Keys.Backspace))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

           

        }
    }
}
