using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine;

using Spacebox.Game.GUI;
using Spacebox.Game;
using Spacebox.Game.GUI.Menu;
using ImGuiNET;
using Spacebox.Game.Generation;
using Engine.SceneManagment;
using Engine.UI;
using Engine.Components;
using Engine.Components.Debug;
using Engine.Utils;
using Spacebox.Game.Player;
using Engine.Physics;
using Spacebox.Game.Resource;
using Engine.Light;


namespace Spacebox.Scenes
{
    public class MenuScene2 : Scene
    {
        private FreeCamera player;
        private FreeCamera player2;

        public MenuScene2(string[] args) : base(args)
        {
          
        }
        SpotLight flashlight;
        public override void LoadContent()
        {
            int x = 20;
            Lighting.FogColor = new Vector3(0.1f);
            Lighting.FogDensity = 0.08f;
            Lighting.AmbientColor = new Vector3(0.4f);
            //Theme.ApplySpaceboxTheme();
            var skyboxTexture = new SpaceTexture(512, 512, World.Seed);
            AddChild(new Skybox(skyboxTexture)).IsAmbientAffected = false;
           player = AddChild(new FreeCamera(new Vector3(0, 0, 5)));
            player2 = AddChild(new FreeCamera(new Vector3(x, 5, 5)));
           
            var cubeRenderer = new CubeRenderer(new Vector3(1,0,1));
            cubeRenderer.AttachComponent(new SphereCollider());
            cubeRenderer.Color = Color4.Green;
            cubeRenderer.AttachComponent(new RotatorComponent(new Vector3(0, 30, 0)));
  
            var spacer = AddChild(new Spacer(new Vector3(x,2,0)));
            AddChild(cubeRenderer);
            var f1 = new CubeRenderer(new Vector3(x,-1,0));
            f1.AttachComponent(new OBBCollider());
            AddChild(f1);

            var c1 = new CubeRenderer(new Vector3(x-3, 0, 0));
            var c2 = new CubeRenderer(new Vector3(1, 0, 0));
            
            c1.Color = Color4.DeepPink;
            c2.Color = Color4.Pink ;
            c1.AddChild(c2);
            //AddChild(c1);
            c2.AttachComponent(new RotatorComponent(new Vector3(0, -30, 0)));
            c2.AttachComponent(new OBBCollider());
            c1.AttachComponent(new OBBCollider());
            //c1.RotateAround(new Vector3(x, 0, 0), Vector3.UnitY, speed * 1);

         
            Node3D model = new Node3D(new Vector3(x+5,0,0));
            Model m = new Model(GenMesh.CreateSphere(2), new TextureMaterial(skyboxTexture));
            m.Material.Color = Color4.Red;
            model.AttachComponent(new ModelRendererComponent(m));
            model.AttachComponent(new SphereCollider());    
            AddChild(model);


            Engine.Texture2D skyboxTexture2 = Resources.Load<Engine.Texture2D>("Resources/Textures/arSphere.png");
            Node3D sphere = new Node3D(new Vector3(0, 0, 1));
            var mat = new FadeMaterial(skyboxTexture2); 
            Model m2 = new Model(GenMesh.CreateSphere(6), mat);
            m2.Material.Color = Color4.White;
            sphere.AttachComponent(new ModelRendererComponent(m2));
            sphere.AttachComponent(new SphereCollider());
            spacer.AddChild(sphere);
            
            var itemTexture = Resources.Load<Texture2D>("Resources/Textures/UI/trash.png");
            itemTexture.FilterMode = FilterMode.Nearest;

            var modelDepth = 0.5f;
            Mesh item = ItemModelGenerator.GenerateMeshFromTexture(itemTexture,  modelDepth);
 
            Node3D itemModel = new Node3D(new Vector3(1, 1, 1));
            var cm = itemModel.AttachComponent(new ModelRendererComponent(new Model(item, new ItemMaterial(itemTexture))));
            cm.Offset = new Vector3(-0.5f, -0.5f, -modelDepth/2f);
            itemModel.AttachComponent(new AxesDebugComponent());
            itemModel.AttachComponent(new OBBCollider());
            //c1.Position = new Vector3(0);
            //spacer.AddChild(c1);

            AddChild(c1);

            var sun = new DirectionalLight { Rotation = new Vector3(45, -30, 0), 
                Intensity = 1 };
            sun.Diffuse = new Color3Byte(209, 201, 157).ToVector3();
            
            sun.Enabled = true;
            AddChild(sun);

            var sun2 = new PointLight { 
                Intensity = 2,
                Range = 7f
           };
            sun2.Position = new Vector3(0,0,0);
            sun2.Diffuse = new Color3Byte(0, 255, 0).ToVector3();
           
            sun2.Enabled = false;
            player2.AddChild(sun2);


            flashlight = new SpotLight
            {
                Constant = 1.0f,
                Linear = 0.09f,
                Quadratic = 0.032f,
                CutOff = MathF.Cos(MathHelper.DegreesToRadians(12.5f)),
                OuterCutOff = MathF.Cos(MathHelper.DegreesToRadians(17.5f)),
                Intensity = 1.0f                          
            };

            player2.AddChild(flashlight);
            flashlight.Position = Vector3.Zero;



            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });
        
        }

      


        public override void Start()
        {
            PrintHierarchy();
        }

        public override void OnGUI()
        {

        }

        public override void UnloadContent()
        {
            
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Update()
        {
           base.Update();
            flashlight.Direction = player2.Front;

            if (Input.IsKeyDown(Keys.R))
            {
                RenderSpace.SwitchSpace();
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