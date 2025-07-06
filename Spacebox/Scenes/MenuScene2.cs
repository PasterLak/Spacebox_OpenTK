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
using Engine.Components;
using Engine.Utils;
using Spacebox.Game.Player;
using Engine.Physics;



namespace Spacebox.Scenes
{
    public class MenuScene2 : Engine.SceneManagment.Scene
    {
        private Skybox skybox;

        private Engine.FreeCamera player;
        private Engine.FreeCamera player2;

        private CubeRenderer cubeRenderer;
        Spacer spacer;
        ColliderComponent vol;
        public MenuScene2(string[] args) : base(args)
        {
          
        }

        public override void LoadContent()
        {
            int x = 20;
            //Theme.ApplySpaceboxTheme();

            float winX = Window.Instance.Size.X;
            float winY = Window.Instance.Size.Y;
            var speed = 15f * Time.Delta;
            //sprite = new Sprite(iso, new Vector2(0, 0), new Vector2(500, 500));
            //GL.Enable(EnableCap.DepthTest);

            
            player = AddChild(new FreeCamera(new Vector3(0, 0, 5)));
            player2 = AddChild(new FreeCamera(new Vector3(x, 5, 5)));
            
            cubeRenderer = new CubeRenderer(new Vector3(0));
            cubeRenderer.AttachComponent(new SphereCollider());
            cubeRenderer.Color = Color4.Green;
            cubeRenderer.AttachComponent(new RotatorComponent(new Vector3(0, 30, 0)));
            spacer = new Spacer(new Vector3(x,2,0));
            AddChild(spacer);
            AddChild(cubeRenderer);
            var f1 = new CubeRenderer(new Vector3(x,-1,0));
            cubeRenderer.AddChild(f1);

            var c1 = new CubeRenderer(new Vector3(x-3, 0, 0));
            var c2 = new CubeRenderer(new Vector3(x, -1, 0));
            
            c1.Color = Color4.Red;
            c1.AddChild(c2);
            AddChild(c1);
            c2.AttachComponent(new RotatorComponent(new Vector3(0, -30, 0)));
            c2.AttachComponent(new OBBCollider());
            vol = c1.AttachComponent(new OBBCollider());
            c1.RotateAround(new Vector3(x, 0, 0), Vector3.UnitY, speed * 1);

            Node3D model = new Node3D();
            Model m = new Model(GenMesh.CreateSphere(2));
            m.Material.Color = Color4.Red;

            model.AttachComponent(new ModelRendererComponent(m));
            model.Position = new Vector3(x+5,0,0);
            AddChild(model);
            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);

            skybox = new Skybox(skyboxTexture);

            skybox.IsAmbientAffected = false;
            AddChild(skybox);

            CenteredImageMenu.LoadImage("Resources/Textures/spaceboxLogo.png", true);

            Resources.Load<AudioClip>("Resources/Audio/UI/click1.ogg");


            var clip = Resources.Load<AudioClip>("Resources/Audio/Music/music.ogg");


            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });
        
        }

      


        public override void Start()
        {
            
            

            // Input.HideCursor(); 
         PrintHierarchy();

        }


        public override void Render()
        {
           //skybox. DrawTransparent(Camera.Main);
            //skybox.Render();
            base.Render();

          
            //spacer.Render();
        }

        public override void OnGUI()
        {
          
           // ImGui.PopFont();
        }

        public override void UnloadContent()
        {
            //skybox.Texture.Dispose();

           // skyboxShader.Dispose();
            
          
        }

        public override void Update()
        {
           base.Update();

            //sprite.UpdateWindowSize(Window.Instance.Size);

            if (Camera.Main.Frustum.IsInFrustum(spacer.OBB.Volume))
            {
               
                    Debug.Log("yes visible " + vol.ToString());
            }
            else
            {
            
                Debug.Log("no visible" + vol.ToString());
            }
            //sprite.UpdateSize(new Vector2(Window.Instance.Size.X, Window.Instance.Size.Y));
            //spacer.Update();
            if (Input.IsKeyDown(Keys.R))
            {
                Camera.Main.CameraRelativeRender = !Camera.Main.CameraRelativeRender;
                //RenderSpace.
            }

            if (Input.IsKeyDown(Keys.T))
            {
               SceneManager.LoadScene(typeof(LogoScene));
            }

            if (Input.IsKeyDown(Keys.C))
            {
               if(player.IsMain)
                {
                    Camera.Main = player2;
                }else
                {
                    Camera.Main = player;
                }
            }

            if (Input.IsKeyDown(Keys.RightControl))
            {
                SceneManager.LoadScene(typeof(MenuScene));
            }

        }
    }
}