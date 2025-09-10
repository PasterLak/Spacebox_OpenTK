using Engine;
using Engine.Audio;
using Engine.Components;
using Engine.InputPro;
using Engine.Light;
using Engine.SceneManagement;
using Engine.UI;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.GUI;


namespace Spacebox.Scenes
{
    public class MenuScene : Scene
    {

        private GameMenu menu;

        private Canvas canvas;

        private DevLogWindow devLogWindow = new DevLogWindow();

        public override void LoadContent()
        {

            //Theme.ApplySpaceboxTheme();

            InputManager.Instance.LoadConfiguration("Resources/default_input.json");

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

            if(Settings.Graphics.EffectsEnabled)
            SetDustSpawner();


            var music = AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/music.ogg"));
            music.PlayOnStart = false;
            if (Settings.Audio.MenuMusic)
            {
                music.Audio.Play();
            }
            else
            {
                music.Audio.IsLooped = false;
                music.Audio.Stop();
            }

            /*var jump = InputManager.Instance.AddAction("inputOverlay", "Jump action");

            jump.AddBinding(new CompositeBinding(true,
                new KeyBinding(Keys.G),
                new KeyBinding(Keys.B)
            ));*/

 
            var jump = InputManager.Instance.GetAction("inputOverlay");
            jump.Subscribe(InputEventType.Pressed, () => InputOverlay.IsVisible = !InputOverlay.IsVisible);

            devLogWindow.AddLogFromFile("Resources/devlog.txt");



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
            dust.FilterMode = FilterMode.Linear;
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

              var colors = new Color4[]
            {
                Color4.Red, Color4.Green, Color4.Blue, Color4.Yellow,
                Color4.Magenta, Color4.Cyan, Color4.Orange, Color4.Purple,
                Color4.Magenta, Color4.Cyan, Color4.Orange, Color4.Purple,
                Color4.Magenta, Color4.Cyan, Color4.Orange, Color4.Purple
            };

            TextureFile.Save("Resources/Textures/test1.txt", colors, false);
            TextureFile.Save("Resources/Textures/test2.txt", colors, false, TextureFile.SaveMode.Binary);
            TextureFile.Save("Resources/Textures/test3.txt", colors, false, TextureFile.SaveMode.Compressed);
            TextureFile.Save("Resources/Textures/test4.txt", colors, false, TextureFile.SaveMode.All);
            TextureFile.Save("Resources/Textures/test1n.txt", colors, false, TextureFile.SaveMode.Text, TextureFile.PaletteMode.None);
            TextureFile.Save("Resources/Textures/test2n.txt", colors, false, TextureFile.SaveMode.Binary, TextureFile.PaletteMode.None);
            TextureFile.Save("Resources/Textures/test3n.txt", colors, false, TextureFile.SaveMode.Compressed, TextureFile.PaletteMode.None);
            TextureFile.Save("Resources/Textures/test4n.txt", colors, false, TextureFile.SaveMode.All, TextureFile.PaletteMode.None);
            TextureFile.Save("Resources/Textures/test1na.txt", colors, false, TextureFile.SaveMode.Text, TextureFile.PaletteMode.Auto);
            TextureFile.Save("Resources/Textures/test2na.txt", colors, false, TextureFile.SaveMode.Binary, TextureFile.PaletteMode.Auto);
            TextureFile.Save("Resources/Textures/test3na.txt", colors, false, TextureFile.SaveMode.Compressed, TextureFile.PaletteMode.Auto);
            TextureFile.Save("Resources/Textures/test4na.txt", colors, false, TextureFile.SaveMode.All, TextureFile.PaletteMode.Auto);
            Input.ShowCursor();
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
            
            CenteredImageMenu.Draw();
           
            VerticalLinks.Draw();
            menu.Render();
            devLogWindow.Render();
            //canvas.Draw();
            // ImGui.PopFont();
            ColorOverlay.OnGUI();
            CenteredText.OnGUI();
        }

        public override void UnloadContent()
        {
            menu.Dispose();
            DevLogWindow.Instance = null;
        }

        bool showed = false;
        public override void Update()
        {
            base.Update();
            CenteredImageMenu.Update();
           

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