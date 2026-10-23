using Engine;
using Engine.Audio;
using Engine.Components;
using Engine.InputPro;
using Engine.Light;
using Engine.SceneManagement;
using Engine.UI;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.GUI;


namespace Spacebox.Scenes
{
    public class MenuScene : Scene
    {

        private GameMenu menu;

        private MenuMusicButton musicButton;
        private Spacebox.Game.GUI.MenuUi.MenuCanvas menuCanvas;

        private Canvas canvas;

        private DevLogWindow devLogWindow = new DevLogWindow();

        private Node3D astronautNode;

        public override void LoadContent()
        {

            //Theme.ApplySpaceboxTheme();

            //modLoader.LoadMods(pluginsPath);

    
            InputManager.Instance.LoadConfiguration("Resources/default_input.json");
            Lighting.ResetToDefaults();

            var player = new CameraStatic(new Vector3(0, 0, 0));
            AddChild(player);
            player.FOV = 100;

            canvas = new Canvas(new Vector2i(1280,720), SpaceboxWindow.Instance);

            var rect = new Rect(new Vector2(0, 0), new Vector2(1280/2f,720/2f));
            rect.Anchor = Engine.UI.Anchor.Center;
            rect.Color4 = Color4.Yellow;
            canvas.AddChild(rect);
            

            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);
      
            Lighting.Skybox = new Skybox( skyboxTexture);
            CenteredImageMenu.LoadImage("Resources/Textures/UI/spaceboxLogo.png", true);

           // Resources.Load<AudioClip>("Resources/Audio/UI/click2.ogg", true);
            menu = new GameMenu();

            SetDustSpawner();


            var music = AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/menu.ogg"));
            music.PlayOnStart = false;
            var musicController = new MenuMusicController(music.Audio);
            musicButton = new MenuMusicButton(musicController, menu.Click1);
            menuCanvas = new Spacebox.Game.GUI.MenuUi.MenuCanvas(menu, musicController, CenteredImageMenu.Texture);

            /*var jump = InputManager.Instance.AddAction("inputOverlay", "Jump action");

            jump.AddBinding(new CompositeBinding(true,
                new KeyBinding(Keys.G),
                new KeyBinding(Keys.B)
            ));*/

 

            devLogWindow.AddLogFromFile("Resources/devlog.txt");


            TextureMaterial mat = new TextureMaterial(Astronaut.GetAstronautTexture("Yellow"));

             astronautNode = new Node3D();
            astronautNode.Name = "Astronaut_Menu";

            var meshBody = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Body_Fly.obj");
            var meshHelmet = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Helmet_Closed.obj");
            var meshTank = Resources.Load<Engine.Mesh>("Resources/Models/Player/Astronaut_Tank_Fly.obj");

            astronautNode.AttachComponent(new ModelRendererComponent(new Model(meshBody, mat)));
            astronautNode.AttachComponent(new ModelRendererComponent(new Model(meshHelmet, mat)));
            astronautNode.AttachComponent(new ModelRendererComponent(new Model(meshTank, mat)));

            AddChild(astronautNode);

            astronautNode.Position = new Vector3(4.3f, 0, -3);
            astronautNode.Rotation = new Vector3(0, 180, 0);

            menu.Get<MultiplayerWindow>().LoadConfig();


        }

        public void ChangeAstronautColor(string color)
        {
            var astronaut = FindChildByName("Astronaut_Menu");
            var texture = Astronaut.GetAstronautTexture(color);
            astronaut.GetComponents<ModelRendererComponent>().ForEach(c =>
            {
                var mat = c.Model.Material as TextureMaterial;
                mat.MainTexture = texture;
            });
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

            var dust = Resources.Load<Texture2D>("Resources/Textures/Effects/dust.png");
            dust.FilterMode = FilterMode.Nearest;
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
            ColorOverlay.FadeOut(new System.Numerics.Vector3(0,0,0), 1);
            VerticalLinks.Init();
            VerticalLinks.IsVisible = true;
            CenteredImageMenu.Show();
              // _ = WorkerPoolTest.RunTest();

            Input.ShowCursor();

            ResetImGuiFont();

            Settings.ShowInterface = false; // for hide + CenteredText, todo rewrite


        }

        public unsafe static void ResetImGuiFont()
        {
            SettingsApplier.ApplyInterface(Settings.Interface);
        }


        public override void Render()
        {

            base.Render();
            menu.RenderOffscreen();

            if(Input.IsKeyDown(Keys.J) && Input.IsKey(Keys.LeftControl) && !ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
            {
                SceneManager.Load<ServerMessageScene>();
            }
        
         

        }

        public override void OnGUI()
        {
          
            Theme.ApplySpaceboxTheme();
            // ImFontPtr myFont = ImGui.GetIO().Fonts.Fonts[1];
            // ImGui.PushFont(myFont);
            BlackScreenOverlay.OnGUI();

            if (ConnectionScreen.IsVisible && menuCanvas != null)
            {
                menuCanvas.Render();
                ColorOverlay.OnGUI();
                return;
            }

            if (ConnectionScreen.IsVisible)
            {
                ConnectionScreen.OnGUI();
                AuthPromptWindow.OnGUI();
                ColorOverlay.OnGUI();
                return;
            }
            
            menuCanvas?.Render();
            if (menuCanvas == null)
            {
                CenteredImageMenu.Draw();
                VerticalLinks.Draw();
                musicButton.Draw();
            }
            menu.Render();
            //devLogWindow.Render();
            //canvas.Draw();
            // ImGui.PopFont();
            ColorOverlay.OnGUI();

            //CenteredText.Hide();
            InputOverlay.OnGUI();
        }

        public override void UnloadContent()
        {
            menuCanvas?.Dispose();
            menuCanvas = null;
            menu.Dispose();
            DevLogWindow.Instance = null;
            Settings.ShowInterface = true;
        }

        bool showed = false;
        public override void Update()
        {
            base.Update();
            CenteredImageMenu.Update();
            menuCanvas?.Update(Time.Delta);
            InputOverlay.Update();

            if(menu.CurrentWindowType == typeof(MultiplayerWindow))
            {
                astronautNode.Enabled = true;
            }else
                        {
                astronautNode.Enabled = false;
            }

            if (Input.IsAnyKeyDown() && !showed)
            {
                CenteredImageMenu.ShowText = false;
                GameMenu.IsVisible = true;
                VerticalLinks.IsVisible = true;
                devLogWindow.IsVisible = true;
                showed = true;
            }

        }
    }
}