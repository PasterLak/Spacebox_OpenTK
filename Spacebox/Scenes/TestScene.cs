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



namespace Spacebox.Scenes
{
    public class TestScene : Engine.SceneManagment.Scene
    {
        private Skybox skybox;

        private Shader skyboxShader;
        private FreeCamera player;
        private DustSpawner spawner;
        private AudioSource music;

        private Axes axes;
        CubeParent cube;

        public TestScene(string[] args) : base(args)
        {

        }

        public override void LoadContent()
        {
            // GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);
            //Theme.ApplySpaceboxTheme();

            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;

            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            player = new FreeCamera(new Vector3(0, 0, 5));
            player.DepthNear = 0.01f;
            player.CameraRelativeRender = false;

              skyboxShader = ShaderManager.GetShader("Shaders/skybox");

            Texture2D skyboxTexture = new SpaceTexture(512, 512, World.Seed);
            skyboxTexture = TextureManager.GetTexture("Resources/Textures/space.png");

            skybox = new Skybox("Resources/Models/sphere.obj", skyboxShader, skyboxTexture);

            skybox.IsAmbientAffected = false;

            axes = new Axes(new Vector3(0, 0, 0), 100);
            SetDustSpawner();

            music = new AudioSource(SoundManager.GetClip("music.ogg"));
            music.IsLooped = true;

           //music.Play();

            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });

            cube = new CubeParent();
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

            Input.ShowCursor();


        }


        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
           

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

            NodeUI.Render(cube.Children);
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
            cube.Update();
            spawner.Update();
            //sprite.UpdateWindowSize(Window.Instance.Size);
            player.Update();
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));


            if(Input.Mouse.ScrollDelta.Y > 0 )
            {
                player.FOV += 1;
            }
            if (Input.Mouse.ScrollDelta.Y < 0)
            {
                player.FOV -= 1;
            }

            if (Input.IsKeyDown(Keys.N) )
            {
                NodeUI.IsVisible = !NodeUI.IsVisible;
            }

                if (Input.IsKeyDown(Keys.Enter) || Input.Mouse.IsButtonDown(MouseButton.Left))
            {
                CenteredImageMenu.ShowText = false;
                GameMenu.IsVisible = true;
                VerticalLinks.IsVisible = true;
            }

        }
    }
}