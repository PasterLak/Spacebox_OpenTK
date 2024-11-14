using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Common.SceneManagment;
using Spacebox.Entities;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.GUI;


namespace Spacebox.Scenes
{
    internal class SpaceMenuScene : Scene
    {

        Skybox skybox;
       
        private Shader skyboxShader;
        private Camera player;

        private DustSpawner spawner;

        AudioSource music;
        private GameMenu menu;

        public SpaceMenuScene(string[] args) : base(args)
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

            player = new Astronaut(new Vector3(0,0,0));

            skyboxShader = ShaderManager.GetShader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader,
                new SpaceTexture(512, 512));
            skybox.IsAmbientAffected = false;


            CenteredImage.LoadImage("Resources/Textures/spaceboxLogo.png", true);
            menu = new GameMenu();

            SetDustSpawner();

            music = new AudioSource(SoundManager.GetClip("music"));
            music.IsLooped = true;
         
            music.Play();

            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => 
            { InputOverlay.IsVisible = !InputOverlay.IsVisible; });


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
            // Input.HideCursor(); 
            
        }
      

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);


            skybox.DrawTransparent(player);
            spawner.Render();
         

        }

        public override void OnGUI()
        {
            Input.ShowCursor();
            CenteredImage.Draw();
            menu.Render();
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

            if (Input.IsKeyDown(Keys.Enter) || Input.Mouse.IsButtonDown(MouseButton.Left))
            {
                CenteredImage.ShowText = false;
                GameMenu.IsVisible = true;
            }

            if (Input.IsKeyDown(Keys.KeyPadEnter))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

           

        }
    }
}
