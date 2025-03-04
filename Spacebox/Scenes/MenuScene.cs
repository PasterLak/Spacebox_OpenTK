using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;
using Spacebox.Game.Effects;
using Engine.Audio;
using Spacebox.Game.GUI;
using Spacebox.Game;
using Spacebox.Game.GUI.Menu;
using ImGuiNET;
using Spacebox.Game.Generation;
using Engine.SceneManagment;

namespace Spacebox.Scenes
{
    public class MenuScene : Engine.SceneManagment.Scene
    {
        private Skybox skybox;

        private Camera player;
        private DustSpawner spawner;
        private AudioSource music;
        private GameMenu menu;

        public MenuScene(string[] args) : base(args)
        {
          
        }

        public override void LoadContent()
        {
            // GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.ClearColor(0, 0, 0, 0);
            //Theme.ApplySpaceboxTheme();

            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;
    
            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            player = new CameraBasic(new Vector3(0, 0, 0));

        

            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);

            skybox = new Skybox("Resources/Models/cube.obj", skyboxTexture);

            skybox.IsAmbientAffected = false;


            CenteredImageMenu.LoadImage("Resources/Textures/spaceboxLogo.png", true);
            menu = new GameMenu();

            SetDustSpawner();
            
            music = new AudioSource(SoundManager.GetClip("music.ogg"));
            music.IsLooped = true;

            music.Play();

            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });
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


        public override void Start()
        {
            
            

            // Input.HideCursor(); 
            CenteredImageMenu.ShowText = true;
            GameMenu.IsVisible = false;

            HealthColorOverlay.SetActive(new System.Numerics.Vector3(0,0,0), 1);

            VerticalLinks.Init();

        }


        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);


            skybox.DrawTransparent(player);
            spawner.Render();
        }

        public override void OnGUI()
        {
            Theme.ApplySpaceboxTheme();
           // ImFontPtr myFont = ImGui.GetIO().Fonts.Fonts[1];

            // Перед выводом UI-элементов переключаемся на нужный шрифт
           // ImGui.PushFont(myFont);
            Input.ShowCursor();
            CenteredImageMenu.Draw();
            HealthColorOverlay.OnGUI();
            VerticalLinks.Draw();
            menu.Render();

           // ImGui.PopFont();
        }

        public override void UnloadContent()
        {
            //skybox.Texture.Dispose();

           // skyboxShader.Dispose();
            spawner.Dispose();
            music.Dispose();
        }

        public override void Update()
        {
            CenteredImageMenu.Update();
            spawner.Update();
            //sprite.UpdateWindowSize(Window.Instance.Size);

            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));

            if (Input.IsKeyDown(Keys.Enter) || Input.Mouse.IsButtonDown(MouseButton.Left))
            {
                CenteredImageMenu.ShowText = false;
                GameMenu.IsVisible = true;
                VerticalLinks.IsVisible = true;
            }

            if (Input.IsKeyDown(Keys.T))
            {
               SceneManager.LoadScene(typeof(TestScene));
            }

        }
    }
}