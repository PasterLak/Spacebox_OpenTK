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
using Engine.UI;
using Engine.Light;


namespace Spacebox.Scenes
{
    public class MenuScene : Scene
    {
        private Camera player;
        private DustSpawner spawner;
        private AudioSource music;
        private GameMenu menu;

        private Canvas canvas;
        public MenuScene(string[] args) : base(args)
        {
          
        }

        public override void LoadContent()
        {
         
            //Theme.ApplySpaceboxTheme();

            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;
    
            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            player = new CameraBasic(new Vector3(0, 0, 0));
            AddChild(player);   
            canvas = new Canvas(new Vector2i(1280,720), Window.Instance);

            var rect = new Rect(new Vector2(0, 0), new Vector2(1280/2f,720/2f));
            rect.Anchor = Engine.UI.Anchor.Center;
            rect.Color4 = Color4.Yellow;
            canvas.AddChild(rect);
            

            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);

            var mesh = Resources.Load<Mesh>("Resources/Models/cube.obj");
      
            AddChild(new Skybox(mesh, skyboxTexture));
            CenteredImageMenu.LoadImage("Resources/Textures/spaceboxLogo.png", true);

            Resources.Load<AudioClip>("Resources/Audio/UI/click1.ogg");
            menu = new GameMenu();
            
            SetDustSpawner();

            var clip = Resources.Load<AudioClip>("Resources/Audio/Music/music.ogg");

            music = new AudioSource(clip);
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
            CenteredImageMenu.ShowText = true;
            GameMenu.IsVisible = false;
            HealthColorOverlay.SetActive(new System.Numerics.Vector3(0,0,0), 1);
            VerticalLinks.Init();

            Debug.Log("LIGHT in new scene start: " + LightSystem.GetLightsCount);
        }


        public override void Render()
        {
            
            base.Render();
            //skybox.DrawTransparent(player);
            spawner.Render();
        }

        public override void OnGUI()
        {
            Theme.ApplySpaceboxTheme();
           // ImFontPtr myFont = ImGui.GetIO().Fonts.Fonts[1];
           // ImGui.PushFont(myFont);
            Input.ShowCursor();
            CenteredImageMenu.Draw();
            HealthColorOverlay.OnGUI();
            VerticalLinks.Draw();
            menu.Render();
            canvas.Draw();
           // ImGui.PopFont();
        }

        public override void UnloadContent()
        {
            spawner.Dispose();
            music.Dispose();
            menu.Dispose();
        }

        public override void Update()
        {
            base.Update();
            CenteredImageMenu.Update();
            spawner.Update();

            if (Input.IsAnyKeyDown())
            {
                CenteredImageMenu.ShowText = false;
                GameMenu.IsVisible = true;
                VerticalLinks.IsVisible = true;
            }

            if (Input.IsKeyDown(Keys.T))
            {
               SceneManager.LoadScene(typeof(LogoScene));
            }

        }
    }
}