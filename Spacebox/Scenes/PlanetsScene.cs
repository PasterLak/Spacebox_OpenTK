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
using Spacebox.Scenes.Test;
using Spacebox.Game.Player;
using Spacebox.FPS.GUI;
using Engine.GUI;
using Engine.SceneManagement;


namespace Spacebox.Scenes
{
    public class PlanetsScene : Engine.SceneManagement.Scene
    {
        private Skybox2 skybox;

        private FreeCamera player;
        private DustSpawner spawner;
        private AudioSource music;

        private Axes axes;
        CubeParent cube;
        Sprite sprite;
        public PlanetsScene(string[] args)
        {

        }

        public override void LoadContent()
        {
            // GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            //GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
            //Theme.ApplySpaceboxTheme();

            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;

            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            Resources.LoadAllAsync<Texture2D>(
               new[]
               {
                    "Resources/Textures/Space/planet2.png",
                    "Resources/Textures/Space/planet3.png",
                    "Resources/Textures/Space/sun.png",
                     "Resources/Textures/Space/skybox2.png",
                      "Resources/Textures/Space/moon.png",
                    "Resources/Textures/Space/space.png"
               });
            Resources.LoadAllAsync<Engine.Mesh>(
              new[]
              {
                    "Resources/Models/sphere2.obj",

              });
            Resources.LoadAllAsync<AudioClip>(
               new[]
               {
                    "Resources/Audio/music.ogg"

               });

            player = new FreeCamera(new Vector3(0, 0, 5));
            player.DepthNear = 0.01f;
            
           // player._cameraRelativeRender = false;

            sprite = new Sprite(Resources.Load<Texture2D>("Resources/Textures/planet2"), new Vector2(0, 0),
                 new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));

            Texture2D skyboxTexture = new SpaceTexture(512, 512, World.Seed);
            skyboxTexture = Resources.Load<Texture2D>("Resources/Textures/space");

            var mesh = Resources.Load<Engine.Mesh>("Resources/Models/sphere.obj");
            skybox = new Skybox2(mesh, skyboxTexture);

            skybox.IsAmbientAffected = false;

            axes = new Axes(new Vector3(0, 0, 0), 100);
            SetDustSpawner();

            music = new AudioSource(Resources.Load<AudioClip>("music"));
            music.IsLooped = true;

            //music.Play();

            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });

            cube = new CubeParent();
            //cube.Position = new Vector3(0, 0, 50000);
            //player.Position = cube.Position;
        }

        private void SetDustSpawner()
        {
            spawner = new DustSpawner(player);


            spawner.ParticleSystem.MaxParticles = 1000;
            spawner.ParticleSystem.SpawnRate = 200;
            spawner.Position = new Vector3(0, 0, 0);


            var emitter = new Emitter(spawner.ParticleSystem)
            {
                SpeedMin = 0f,
                SpeedMax = 0f,
                LifetimeMin = 100f,
                LifetimeMax = 1000f,
                SizeMin = 0.01f,
                SizeMax = 0.1f,
                StartColorMin = new Vector4(1f, 1f, 1f, 1f),
                StartColorMax = new Vector4(1f, 1f, 1f, 1f),
                EndColorMin = new Vector4(1f, 1f, 1f, 0f),
                EndColorMax = new Vector4(0.9f, 0.9f, 0.9f, 0f),
                SpawnRadius = 100f,
                EmitterDirection = new Vector3(0, 0, 0),
            };
            spawner.ParticleSystem.UseLocalCoordinates = false;
            spawner.ParticleSystem.Emitter = emitter;
        }


        public override void Start()
        {

            Input.HideCursor();

        }


        public override void Render()
        {
            // GL.Clear(ClearBufferMask.ColorBufferBit);

            sprite.Render(new Vector2(0, 0), new Vector2(1, 1));
            //player
            skybox.DrawTransparent(player);
            spawner.Render();
            //axes.Render(player);
           
            cube.Render();
        }

        public override void OnGUI()
        {
            // Theme.ApplySpaceboxTheme();

            // ImGui.PopFont();

            // NodeUI.Render(cube.Children);
        }

        public override void UnloadContent()
        {

            if(VisualDebug.Enabled)
            axes.Dispose();

            spawner.Dispose();
            music.Dispose();
            sprite.Dispose();
            cube.Dispose();
        }

        public override void Update()
        {
            cube.Update();
            spawner.Update();
            //sprite.UpdateWindowSize(Window.Instance.Size);
            player.Update();
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            // sprite.UpdateWindowSize(Window.Instance.ClientSize);
            // sprite.UpdateSize(Window.Instance.Size);
         
            if (Input.Mouse.ScrollDelta.Y > 0)
            {
                player.FOV += 1;
            }
            if (Input.Mouse.ScrollDelta.Y < 0)
            {
                player.FOV -= 1;
            }
         
            if (Input.IsKeyDown(Keys.N))
            {
                NodeUI.IsVisible = !NodeUI.IsVisible;
            }


        }
    }
}