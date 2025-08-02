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
using Engine.SceneManagement;
using Engine.UI;
using Engine.Light;
using Spacebox.GUI;
using Engine.Components;
using Engine.Multithreading;


namespace Spacebox.Scenes
{
    public class MenuScene : Scene
    {

        private GameMenu menu;

        private Canvas canvas;

        public override void LoadContent()
        {
         
            //Theme.ApplySpaceboxTheme();

            var player = new CameraStatic(new Vector3(0, 0, 0));
            AddChild(player);
            player.FOV = 100;

            canvas = new Canvas(new Vector2i(1280,720), Window.Instance);

            var rect = new Rect(new Vector2(0, 0), new Vector2(1280/2f,720/2f));
            rect.Anchor = Engine.UI.Anchor.Center;
            rect.Color4 = Color4.Yellow;
            canvas.AddChild(rect);
            

            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);

            var mesh = Resources.Load<Mesh>("Resources/Models/cube.obj");
      
            Lighting.Skybox = new Skybox(mesh, skyboxTexture);
            CenteredImageMenu.LoadImage("Resources/Textures/spaceboxLogo.png", true);

            Resources.Load<AudioClip>("Resources/Audio/UI/click1.ogg");
            menu = new GameMenu();
            
            SetDustSpawner();

            AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/music.ogg"));

           
            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });
        
        }

        private void SetDustSpawner()
        {
    
            var emitter = new PlaneEmitter
            {
                SpeedMin = 3,
                SpeedMax = 8,
                LifeMin = 7f,
                LifeMax = 10f,
                StartSizeMin = 0.05f,
                StartSizeMax = 0.1f,
                EndSizeMin = 0.1f,
                EndSizeMax = 0.2f,
                AccelerationStart = new Vector3(0f, 0f, 0f),
                AccelerationEnd = new Vector3(0f, 0f, 0f),
                RotationSpeedMin = 0f,
                RotationSpeedMax = 180f,
                ColorStart = new Vector4(0.5f, 0.5f, 0.5f, 0.7f),
                ColorEnd = new Vector4(1f, 1f, 1f, 1f),
                Center = new Vector3(0f, 0f, 0f),
                Normal = new Vector3(0f, 1f, 0f),
                Width = 70f,
                Height = 70f,
                Direction = new Vector3(0f, 1f, 0f),
            };

            var dust = Resources.Load<Texture2D>("Resources/Textures/dust.png");
            var system = new ParticleSystem(new ParticleMaterial(dust), emitter);
            system.Max = 500;
            system.Rate = 70f;
            system.Space = SimulationSpace.Local;
            system.Position = new Vector3(0,0,-30);
            system.Rotation = new Vector3(90,0,0);

            system.Prewarm(50f);
            AddChild(system);
           
        }


        public override void Start()
        {
            base.Start();
            CenteredImageMenu.ShowText = true;
            GameMenu.IsVisible = false;
            HealthColorOverlay.SetActive(new System.Numerics.Vector3(0,0,0), 1);
            VerticalLinks.Init();

           // _ = WorkerPoolTest.RunTest();
        }


        public override void Render()
        {

            base.Render();
 
        }

        public override void OnGUI()
        {
          
            Theme.ApplySpaceboxTheme();
            // ImFontPtr myFont = ImGui.GetIO().Fonts.Fonts[1];
            // ImGui.PushFont(myFont);
            BlackScreenOverlay.OnGUI();
            Input.ShowCursor();
            CenteredImageMenu.Draw();
            HealthColorOverlay.OnGUI();
            VerticalLinks.Draw();
            menu.Render();
            //canvas.Draw();
           // ImGui.PopFont();
        }

        public override void UnloadContent()
        {
            menu.Dispose();
        }

        public override void Update()
        {
            base.Update();
            CenteredImageMenu.Update();

            if (Input.IsAnyKeyDown())
            {
                CenteredImageMenu.ShowText = false;
                GameMenu.IsVisible = true;
                VerticalLinks.IsVisible = true;
            }

            if (Input.IsKeyDown(Keys.T))
            {
               
                SceneManager.Reload();
            }

            if (Input.IsKeyDown(Keys.P))
            {
               // BlackScreenOverlay.IsEnabled = true;
                //SceneManager.Load<ParticleSystemEditor>();
            }

        }
    }
}