using Spacebox.Common.SceneManagment;
using OpenTK.Mathematics;
using Spacebox.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.GUI;
using Spacebox.Game;
using Spacebox.Common.Audio;
using Spacebox.Game.Commands;
using Spacebox.UI;
using Spacebox.Game.GUI;
using Spacebox.Common.Physics;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Effects;
using Spacebox.Game.Resources;
using Spacebox.Common.GUI;
using Spacebox.common.Animation;
using Spacebox.Common.Animation;
using static Spacebox.Game.WorldLoader;

namespace Spacebox.Scenes
{
    internal class SpaceScene : Scene
    {
        private Astronaut player;
        private Skybox skybox;
        private World world;
        private Shader skyboxShader;


        //private Sector0 sector;
        private Shader blocksShader;
        private Texture2D blockTexture;
        private Texture2D lightAtlas;
        // to base

        //public static AudioSource blockPlace;
        //public static AudioSource blockDestroy;

        public static AudioSource Death;
        public static bool DeathOn = false;
        public static AudioSource Uii;
        private AudioSource ambient;
        private DustSpawner dustSpawner;


        private BlockDestructionManager blockDestructionManager;

        private ItemModel itemModel;
        private Shader itemModelShader;
        private HealthBar healthBar;
        private BlockSelector blockSelector;


        private RadarWindow radarWindow;
        public static Model spacer;
        private string worldName;

        private SimpleBlock block1;
        private SimpleBlock block2;

        private ToggleManager toggleManager;

        public SpaceScene(string[] args) : base(args) // name mod seed modfolder
        {
            if (args.Length == 4)
            {
                worldName = args[0];

                string modsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.GameSet.Folder);
                string blocksPath = Path.Combine(modsFolder, args[3], Globals.GameSet.Blocks);
                string itemsPath = Path.Combine(modsFolder, args[3], Globals.GameSet.Items);
                string emissionPath = Path.Combine(modsFolder, args[3], Globals.GameSet.Emissions);

                Debug.Log("block textures: " + modsFolder);
                if (GameBlocks.IsInitialized)
                {
                    if (GameBlocks.modId.ToLower() == args[1].ToLower())
                    {
                    }
                    else
                    {
                        GameBlocks.DisposeAll();

                        InitializeGamesetData(blocksPath, itemsPath, emissionPath, args[1], 32);
                    }
                }
                else
                {
                    InitializeGamesetData(blocksPath, itemsPath, emissionPath, args[1], 32);
                }

                if (int.TryParse(args[2], out var result))
                {
                    World.Random = new Random(result);
                }
                else
                {
                    World.Random = new Random();
                    Debug.Error("Wrong seed format! Seed: " + args[2]);
                }
            }

            BlackScreenOverlay.IsEnabled = true;
            BlackScreenOverlay.Render();

            toggleManager = new ToggleManager();

            var mouse = toggleManager.Register("mouse");

            mouse.OnStateChanged += s =>
            {
                if (s) Input.ShowCursor(); else Input.HideCursor();
            };
        }

        private void InitializeGamesetData(string blocksPath, string itemsPath, string emissionPath, string modId,
            byte blockSizePixels)
        {
            GameBlocks.AtlasBlocks = new AtlasTexture();
            GameBlocks.AtlasItems = new AtlasTexture();

            var texture = GameBlocks.AtlasBlocks.CreateTexture(blocksPath, blockSizePixels, false);
            var items = GameBlocks.AtlasItems.CreateTexture(itemsPath, blockSizePixels, false);
            var emissions = GameBlocks.AtlasBlocks.CreateEmission(emissionPath);

            GameBlocks.BlocksTexture = texture;
            GameBlocks.ItemsTexture = items;
            GameBlocks.LightAtlas = emissions;

            GameSetLoader.Load(modId);

            GameBlocks.BlocksTexture = texture;
            GameBlocks.ItemsTexture = items;
            GameBlocks.LightAtlas = emissions;

            //texture.SaveToPng($"blocks.png");
            //items.SaveToPng($"items.png");
            //emissions.SaveToPng($"emissions.png");

            GameBlocks.IsInitialized = true;
        }

        public override void LoadContent()
        {
            skyboxShader = ShaderManager.GetShader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader,
                new SpaceTexture(512, 512, World.Random));
            skybox.Scale = new Vector3(Settings.ViewDistance, Settings.ViewDistance, Settings.ViewDistance);
            skybox.IsAmbientAffected = false;
            SceneGraph.AddRoot(skybox);

            radarWindow = new RadarWindow(skybox.Texture);

            player = new Astronaut(new Vector3(5,5,5));
            SceneGraph.AddRoot(player);
            
            World.LoadWorldInfo(worldName);
            world = new World(player);
            
            player.GameMode = World.Data.Info.GameMode;
            PlayerSaveLoadManager.LoadPlayer(player, World.Data.WorldFolderPath);


           

            CollisionManager.Add(player);

            

            Death = new AudioSource(SoundManager.GetClip("death2"));
            Uii = new AudioSource(SoundManager.GetClip("uii"));
            

            ambient = new AudioSource(SoundManager.GetClip("Music/ambientMain"));
            ambient.IsLooped = true;
            ambient.Volume = 0.05f;
            ambient.Play();

            var e = Overlay.GetElementByType(typeof(CameraElement));

            if (e != null)
            {
                e.ElementAfter = new AstronautOverlayElement();
            }

            Overlay.AddElement(new AImedBlockElement());

            Input.SetCursorState(CursorState.Grabbed);

            //chunk = new Chunk(new Vector3(0,0,0));
            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () => { InputOverlay.IsVisible = !InputOverlay.IsVisible; });


            blocksShader = ShaderManager.GetShader("Shaders/block");
            blockTexture = GameBlocks.BlocksTexture;

            lightAtlas = GameBlocks.LightAtlas;

            blocksShader.Use();
            blocksShader.SetInt("texture0", 0);
            blocksShader.SetInt("textureAtlas", 1);

            blocksShader.SetFloat("fogDensity", Lighting.FogDensity);
            blocksShader.SetVector3("fogColor", Lighting.FogColor);
            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);

            Texture2D block = UVAtlas.GetBlockTexture(blockTexture, 0, 0, GameBlocks.AtlasBlocks.SizeBlocks);

            //chunk.RemoveBlock(0,0,0);

            

            blockDestructionManager = new BlockDestructionManager(player);

            //sector = new Sector0(new Vector3(0, 0, 0), new Vector3i(0, 0, 0), world);

            dustSpawner = new DustSpawner(player);

            healthBar = new HealthBar();

            //builder.AddCube(Vector3.Zero, CubeType.Wireframe, Color4.Yellow, new Vector2(0,0));

            Debug.RegisterCommand(new TeleportCommand(player));
            Debug.RegisterCommand(new TagCommand(player));
            Debug.RegisterCommand(new ClearInventoryCommand(player));
            Debug.RegisterCommand(new GameModCommand(player));
            Debug.RegisterCommand(new SpawnAroundAsteroidCommand(player));

            Texture2D c = TextureManager.GetTexture("Resources/Textures/slot.png", true, false);
            Texture2D c2 = TextureManager.GetTexture("Resources/Textures/selectedSlot.png", true, false);
            InventoryUI.Initialize(c.Handle);
            PanelUI.Initialize(player, c.Handle, c2.Handle);

            InventoryUI.Player = player;

            CreativeWindowUI.SetDefaultIcon(c.Handle, player);
          
            itemModel = ItemModelGenerator.GenerateModel(GameBlocks.ItemsTexture, 2, 2, 0.05f, 0.5f, false);
            itemModel.Position = new Vector3(0, 0, 0);
            itemModel.debug = true;
            //player.AddChild(itemModel);
            itemModelShader = ShaderManager.GetShader("Shaders/itemModel");

            blockSelector = new BlockSelector();

            var tex = TextureManager.GetTexture("Resources/Textures/spacer.png");
            tex.FlipY();
            tex.UpdateTexture(true);
            spacer = new Model("Resources/Models/spacer.obj",
                new Material(ShaderManager.GetShader("Shaders/textured"), tex));

            spacer.Position = player.Position + new Vector3(12, 15, 7);
            spacer.Rotation = new Vector3(0, 0, 0);
            //Renderer.AddDrawable(spacer);
            //SceneGraph.AddRoot(spacer);
            //SceneGraph.UpdateTransforms();

            block1 = new SimpleBlock(ShaderManager.GetShader("Shaders/colored"),
                TextureManager.GetTexture("Resources/Textures/slot.png"), new Vector3(0, 0, 0));
            block1.Position = new Vector3(-1, -1, 0);
            //SceneGraph.AddRoot(block1);
            //Renderer.AddDrawable(block1);

            block2 = new SimpleBlock(ShaderManager.GetShader("Shaders/colored"),
                TextureManager.GetTexture("Resources/Textures/slot.png"), new Vector3(0, 0, 0));

            //block1.AddChild(block2);
            //Renderer.AddDrawable(block1);
            //block2.Position = new Vector3(2, 0, 0);
            //SceneGraph.PrintHierarchy();

            WelcomeUI.OnPlayerSpawned(World.Data.Info.ShowWelcomeWindow);
            WelcomeUI.Init();
        }
        Animator animator;
        public override void Start()
        {
            Input.HideCursor();
            BlackScreenOverlay.IsEnabled = false;

            Time.OnTick += () =>
            {
                TickTaskManager.UpdateTasks();
            };
            CraftingGUI.Init();

            animator = new Animator(spacer);
            animator.AddAnimation(new MoveAnimation(spacer.Position, spacer.Position + new Vector3(0,0,1000), 5000f, false));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));
        }

        public override void Update()
        {
            Time.HandleTicks();
            
            player.Update();
            blockDestructionManager.Update();
            dustSpawner.Update();
            //itemModel.Rotation = player.Rotation;
            world.Update();
            if (!Debug.IsVisible)
            {
                if (Input.IsKeyDown(Keys.KeyPadEnter))
                {
                    SceneManager.LoadScene(typeof(SpaceMenuScene));
                }

                if (Input.IsKeyDown(Keys.F8))
                {
                    Settings.ShowInterface = !Settings.ShowInterface;
                }

                if (Input.IsKeyDown(Keys.KeyPad7))
                {
                    SceneGraph.PrintHierarchy();
                }
            }

            //model.Position = player.Position;
            if (DeathOn)
            {
                if (!Death.IsPlaying)
                {
                    Window.Instance.Quit();
                }
            }

            if (Input.IsKeyDown(Keys.B))
            {
                // Uii.Play();
            }

            dustSpawner.Update();
            PanelUI.Update();

            //chunk.Test(player);
            animator.Update(Time.Delta);

            //world.Update();

            //sector.Update();
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            DisposalManager.ProcessDisposals();

            skybox.DrawTransparent(player);


            GL.Enable(EnableCap.DepthTest);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = player.GetViewMatrix();
            Matrix4 projection = player.GetProjectionMatrix();

            //blocksShader.SetMatrix4("model", model);
            blocksShader.SetMatrix4("view", view);
            blocksShader.SetMatrix4("projection", projection);

            var position = player.CameraRelativeRender ? Vector3.Zero : player.Position;
            blocksShader.SetVector3("cameraPosition", position);

            blocksShader.SetVector3("ambientColor", Lighting.AmbientColor);


            blockTexture.Use(TextureUnit.Texture0);
            lightAtlas.Use(TextureUnit.Texture1);


            //chunk.Draw(blocksShader);
            player.Draw();
            //sector.Render(blocksShader);
            world.Render(blocksShader);
            //itemModel.Draw(itemModelShader);
            //world.Render(blocksShader);
            blockDestructionManager.Render();

            //spacer.LookAt3(player);
            spacer.Draw(player);
            dustSpawner.Render();
            if (InteractionDestroyBlockSurvival.BlockMiningEffect != null)
            {
                InteractionDestroyBlockSurvival.BlockMiningEffect.Render();
            }

            // block1.Rotate(0,12f * Time.Delta,0);
            //block1.Render(player);
            // block2.Render(player);


            var b = new BoundingSphere(block2.GetWorldPosition(), 0.7f);
            VisualDebug.DrawBoundingSphere(b, Color4.AliceBlue);
            blockSelector.Draw(player);


            GL.Disable(EnableCap.DepthTest);


            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            PanelUI.DrawItemModel();
            //sprite.Render(new Vector2(0, 0), new Vector2(1, 1));
            const float sm = 0.02f;
            VisualDebug.DrawLine(Vector3.Zero + new Vector3(sm, sm, sm), new Vector3(100f, 0, 0), Color4.Red);
            VisualDebug.DrawLine(Vector3.Zero + new Vector3(sm, sm, sm), new Vector3(0, 100, 0), Color4.Green);
            VisualDebug.DrawLine(Vector3.Zero + new Vector3(sm, sm, sm), new Vector3(0, 0, 100), Color4.Blue);


            //testOctree.Draw(player);
        }


        public override void OnGUI()
        {
            HealthColorOverlay.Render();
            CenteredText.Draw();

            TagText.Draw();

            //healthBar.OnGUI();
            radarWindow.Render();
           
            ResourceProcessingGUI.OnGUI();
           CraftingGUI.OnGUI();
            PanelUI.Render();
            player.OnGUI();
            InventoryUI.OnGUI(player.Inventory);
            CreativeWindowUI.Render();
            WelcomeUI.OnGUI();
            if (VisualDebug.ShowDebug)
            {
                WorldTextDebug.Draw();
            }

            TagManager.DrawTags(player, Window.Instance.Size.X, Window.Instance.Size.Y);

            BlackScreenOverlay.Render();
        }

        public override void UnloadContent()
        {
            //sector.Dispose();
            blocksShader.Dispose();
            //blockTexture.Dispose();
            lightAtlas.Dispose();
            Sector.IsPlayerSpawned = false;
            TickTaskManager.Dispose();
            //music.Dispose();
            skybox.Texture.Dispose();
            skyboxShader.Dispose();
            dustSpawner.Dispose();
            itemModel.Dispose();
            blockDestructionManager.Dispose();
            World.DropEffectManager.Dispose();
            TagManager.ClearTags();
            world.Dispose();
        }
    }
}