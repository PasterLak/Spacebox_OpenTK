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
using Engine.Components;


namespace Spacebox.Scenes
{
    public class PlanetsScene2 : Engine.SceneManagement.Scene
    {
        private Skybox2 skybox;

        private FreeCamera0 player;
        private DustSpawner spawner;
        private AudioSource music;

        private Axes axes;
        CubeParent cube;
        Sprite sprite;


        private Node3D earth = new Node3D();
        private Node3D sun = new Node3D();
        private Node3D atmosphere = new Node3D();
        private Node3D mars = new Node3D();

        private Node3D moon;
   

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

            player = new FreeCamera0(new Vector3(0, 0, 5));
            player.DepthNear = 0.01f;
            
          

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

            InputManager0.AddAction("inputOverlay", Keys.F6);
            InputManager0.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });

            var matEarth = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/planet2"));
            matEarth.GlowColor = new Vector3(0.9f, 1, 1);

            var matMars = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/planet3"));
            matMars.GlowColor = new Vector3(0.9f, 0.5f, 0.5f);

            var atmosphereMat = new AtmosphereMaterial(Resources.Load<Texture2D>("Resources/Textures/skybox2"));
            atmosphereMat.GlowColor = new Vector3(0.9f, 1, 1);

            var matMoon = new PlanetMaterial(Resources.Load<Texture2D>("Resources/Textures/moon"));
            matMoon.GlowColor = new Vector3(0.7f, 0.7f, 0.7f);
            //var sunMat = new ColorMaterial();

            var sunMat = new SunMaterial(Resources.Load<Texture2D>("Resources/Textures/sun"));
            sunMat.GlowColor = new Vector3(1f, 1, 1);
            sunMat.Color = new Color4(1f, 1f, 1f, 1f);


  

            mars.SetScale(0.15f);
            // earth.SetScale(0.2f);
            moon.SetScale(0.2f);
            atmosphere.Scale = Vector3.One * 1.01f;

            earth.AddChild(moon);
            moon.Position = new Vector3(2.5f, 0, 0);

            earth.AddChild(atmosphere);


            sun.SetScale(1);
            sun.AddChild(mars);
            sun.AddChild(earth);
            earth.Position = new Vector3(4, 0, 0);
            mars.Position = new Vector3(-7, 0, 0);
            AddChild(sun);
            //cube.Position = new Vector3(0, 0, 50000);
            //player.Position = cube.Position;
        }

        private void SetDustSpawner()
        {
            spawner = new DustSpawner(player);


            spawner.ParticleSystem.Max = 1000;
            spawner.ParticleSystem.Rate = 200;
            spawner.Position = new Vector3(0, 0, 0);


            var emitter = new EmitterOld(spawner.ParticleSystem)
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
           // spawner.ParticleSystem.UseLocalCoordinates = false;
           // spawner.ParticleSystem.Emitter = emitter;
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
            if (Input.IsKeyDown(Keys.T))
            {
                SceneManager.Load<LogoScene>();
            }
            if (Input.IsKeyDown(Keys.N))
            {
                NodeUI.IsVisible = !NodeUI.IsVisible;
            }


        }
    }
}