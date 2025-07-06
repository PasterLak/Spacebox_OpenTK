using Engine;
using Engine.Animation;
using Engine.Audio;
using Engine.GUI;
using Engine.Light;
using Engine.Physics;
using Engine.SceneManagment;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game;
using Spacebox.Game.Commands;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using Client;
using Engine.Components;
using Spacebox.Game.Player.Interactions;

namespace Spacebox.Scenes
{
    public abstract class BaseSpaceScene : Scene
    {
       
        protected Astronaut localPlayer;
        protected Skybox skybox;
        protected World world;
 
        BlockMaterial blockMaterial;
        protected string worldName;
        protected PointLightsPool pointLightsPool;
        protected BlockSelector blockSelector;
        protected RadarUI radarWindow;

      
        protected AudioSource Death;
        protected static bool DeathOn = false;
        protected AudioSource ambient;

      
        protected DustSpawner dustSpawner;
        protected BlockDestructionManager blockDestructionManager;
        protected Animator animator;
        public static Spacer spacer;
      
        protected PointLight pLight;
        private SpheresPool SpheresPool;
        private bool isMultiplayer = false;
      
        public BaseSpaceScene(string[] args) : base(args)
        {
            if((this as MultiplayerScene) != null)
            {
                isMultiplayer = true;// (worldName/server, modId, seed, modfolder) + ( modfolderName, key, ip, port, nickname)
               
                foreach(var arg in args)
                {
                    Debug.Warning(arg.ToString());
                }
            }

            HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 0, 0), 1);

            //  (worldName, modId, seed, modfolder)
            if (args.Length >= 4)
            {
                worldName = args[0];
                var modFolderName = "";
                var modId = args[1];
                var seedString = args[2];

                if (isMultiplayer)
                {
                    if(ClientNetwork.Instance!= null)
                    {
                        modFolderName = ClientNetwork.Instance.ReceivedServerInfo.ModFolderName;
                    }
                }
                else
                {
                    modFolderName = args[3];
                }

                string serverName = "";

                if(isMultiplayer)
                {
                    serverName = worldName;
                }


                string modsFolder =  ModPath.GetModsPath(isMultiplayer, serverName);
  
                string blocksPath = ModPath.GetBlocksPath(modsFolder, modFolderName);
            
                string itemsPath = ModPath.GetItemsPath(modsFolder, modFolderName);
                string emissionPath = ModPath.GetEmissionsPath(modsFolder, modFolderName);

                if (GameAssets.IsInitialized)
                {
                    if (GameAssets.ModId.ToLower() != modId.ToLower())
                    {
                        GameAssets.DisposeAll();
                        InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName);
                    }
                }
                else
                {
                    InitializeGamesetData(blocksPath, itemsPath, emissionPath, modId, 32, serverName);
                }
                if (int.TryParse(seedString, out var seed))
                {
                    //World.Random = new Random(seed);
                }
                else
                {
                   // World.Random = new Random();
                    Debug.Error("Wrong seed format! Seed: " + seedString);
                }
            }

            BlackScreenOverlay.IsEnabled = true;
            BlackScreenOverlay.OnGUI();

            var mouse = ToggleManager.Register("mouse");
            mouse.OnStateChanged += s =>
            {
                if (s)
                {
                    if (localPlayer != null)
                        localPlayer.ResetMousePosition();
                    Input.MoveCursorToCenter();
                    Input.ShowCursor();
                }
                else
                {
                    if (localPlayer != null)
                        localPlayer.ResetMousePosition();
                    Input.MoveCursorToCenter();
                    Input.HideCursor();
                }
            };

            InputManager.AddAction("inputOverlay", Keys.F6);
            InputManager.RegisterCallback("inputOverlay", () =>
            {
                InputOverlay.IsVisible = !InputOverlay.IsVisible;
            });
        }

        private void InitializeGamesetData(string blocksPath, string itemsPath, string emissionPath, string modId, byte blockSizePixels, string serverName)
        {

            GameAssets.AtlasBlocks = new AtlasTexture();
            GameAssets.AtlasItems = new AtlasTexture();
            var texture = GameAssets.AtlasBlocks.CreateTexture(blocksPath, blockSizePixels, false);
            var items = GameAssets.AtlasItems.CreateTexture(itemsPath, blockSizePixels, false);
            var emissions = GameAssets.AtlasBlocks.CreateEmission(emissionPath);
            GameAssets.BlocksTexture = texture;
            GameAssets.ItemsTexture = items;
            GameAssets.LightAtlas = emissions;
           
            GameSetLoader.Load(modId, isMultiplayer, isMultiplayer ? serverName : "");
            GameAssets.IsInitialized = true;
        }

        public override void LoadContent()
        {

       
            var texture = new SpaceTexture(512, 512, World.Seed);

            var mesh = Resources.Load<Engine.Mesh>("Resources/Models/cube.obj");
            skybox = new Skybox(mesh,  texture);
            skybox.Scale = new Vector3(Settings.ViewDistance, Settings.ViewDistance, Settings.ViewDistance);
            skybox.IsAmbientAffected = false;
            

            radarWindow = new RadarUI(texture);

            if (localPlayer == null)
            {
                localPlayer = new Astronaut(new Vector3(5, 5, 5));
            }

            PanelUI.Player = localPlayer;
          

            World.LoadWorldInfo(worldName);
            world = new World(localPlayer);
          
            PlayerSaveLoadManager.LoadPlayer(localPlayer, World.Data.WorldFolderPath);
            CollisionManager.Add(localPlayer);

            Death = new AudioSource(SoundManager.GetClip("death2"));
            ambient = new AudioSource(SoundManager.GetClip("Music/spaceBackground"));
            ambient.IsLooped = true;
            ambient.Volume = 0.05f;
            ambient.Play();

            var cameraElement = Overlay.GetElementByType(typeof(CameraElement));
            if (cameraElement != null)
            {
                cameraElement.ElementAfter = new AstronautOverlayElement();
            }
            Overlay.AddElement(new AImedBlockElement());

            Input.SetCursorState(CursorState.Grabbed);

           

            localPlayer.GameMode = World.Data.Info.GameMode;
           
            blockMaterial = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.LightAtlas, localPlayer);

            pointLightsPool = new PointLightsPool(blockMaterial.Shader, localPlayer, 64);

            blockDestructionManager = new BlockDestructionManager(localPlayer);
            dustSpawner = new DustSpawner(localPlayer);

      
            Debug.RegisterCommand(new ChatCommand());
            Debug.RegisterCommand(new DebugTexturesCommand());
            Debug.RegisterCommand(new TeleportCommand(localPlayer));
            Debug.RegisterCommand(new TagCommand(localPlayer));
            Debug.RegisterCommand(new ClearInventoryCommand(localPlayer));
            Debug.RegisterCommand(new GameModCommand(localPlayer));
            Debug.RegisterCommand(new SpawnAroundAsteroidCommand(localPlayer));

         
            Texture2D slotTex = Resources.Load<Texture2D>("Resources/Textures/slot.png");
            Texture2D selectedSlotTex = Resources.Load<Texture2D>("Resources/Textures/selectedSlot.png");

            slotTex.FilterMode = FilterMode.Nearest;
            slotTex.FlipY();
            selectedSlotTex.FilterMode = FilterMode.Nearest;

            InventoryUI.Initialize(slotTex.Handle);
            StorageUI.Initialize(slotTex.Handle);
            PanelUI.Initialize(localPlayer, slotTex.Handle, selectedSlotTex.Handle);
            InventoryUI.Player = localPlayer;
            CreativeWindowUI.SetDefaultIcon(slotTex.Handle, localPlayer);

            blockSelector = new BlockSelector();

 
            spacer = new Spacer(localPlayer.Position + new Vector3(5, 5, 7));
            AddChild(spacer);
            //AddChild(localPlayer);
            WelcomeUI.OnPlayerSpawned(World.Data.Info.ShowWelcomeWindow);
            WelcomeUI.Init();
            PauseUI.Init();
            SpheresPool = new SpheresPool();
        }

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
            animator.AddAnimation(new MoveAnimation(spacer.Position, spacer.Position + new Vector3(0, 0, 1000), 5000f, false));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitX, 5f, 0f));
            animator.AddAnimation(new RotateAnimation(Vector3.UnitY, 5f, 0f));

            Debug.OnVisibilityWasChanged += OnDebugStateChanged;
            Window.OnResized += TagManager.OnResized;

       
            pLight = PointLightsPool.Instance.Take();
            pLight.Ambient = new Vector3(1, 0, 0);
            pLight.Position = localPlayer.Position;
            pLight.Range = 25;
            pLight.Ambient = new Color3Byte(100, 116, 255).ToVector3();
            pLight.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
            pLight.Specular = new Color3Byte(100, 116, 255).ToVector3();
          
            pLight.IsActive = false;

            Chat.Write("Welcome to Spacebox!",Color4.Yellow);
        }

        private void OnDebugStateChanged(bool state)
        {
            ToggleManager.SetState("mouse", state);
            ToggleManager.SetState("player", !state);
            ToggleManager.SetState("panel", !state);
            InputManager.Enabled = !state;
            ToggleManager.DisableAllWindows();
            ToggleManager.SetState("radar", false);
            ToggleManager.SetState("inventory", false);
            Input.MoveCursorToCenter();

            if(Chat.IsVisible)
            {
                Chat.IsVisible = false;
            }
        }

        public override void Update()
        {
            Time.HandleTicks();

            var node = this as Node3D;
            node.Update();

            localPlayer.Update();
            blockDestructionManager.Update();
            dustSpawner.Update();
            MainThreadDispatcher.Instance.ExecutePending();
            world.Update();
           
            if (Input.IsKeyDown(Keys.R))
            {
                Camera.Main.CameraRelativeRender = !Camera.Main.CameraRelativeRender;


            }


            if (Camera.Main.Frustum.IsInFrustum(spacer.OBB.Volume))
            {
              
                  //  Debug.Log("yes visible " + spacer.OBB.Volume);
            }
            else
            {
             
                 //   Debug.Log("no visible" + spacer.OBB.Volume);
            }

            if (Input.IsKeyDown(Keys.K))
            {
                /*
                var l = PointLightsPool.Instance.Take();
                l.IsActive = true;
                l.Position = localPlayer.Position;
                l.Range = 20;
                l.Ambient = new Color3Byte(100, 116, 255).ToVector3();
                l.Diffuse = new Color3Byte(100, 116, 255).ToVector3();
                l.Specular = new Color3Byte(0, 0, 0).ToVector3();
              */
            }

            if (Input.IsKeyDown(Keys.Escape))
            {
                if (Chat.IsVisible && Chat.FocusInput)
                {
                 
                    
                    return;
                }

               
                int opened = ToggleManager.OpenedWindowsCount;

                ToggleManager.DisableAllWindows();

                if (opened > 0)
                {
                   // Debug.Log("Opened!");
                    ToggleManager.SetState("inventory", false);
                    ToggleManager.SetState("mouse", false);
                    ToggleManager.SetState("player", true);
                    ToggleManager.SetState("panel", true);
                }
                else
                {
                    ToggleManager.SetState("inventory", false);
                    ToggleManager.SetState("pause", true);
                    ToggleManager.SetState("panel", false);
                }
            }

            pLight.Position = localPlayer.Position;

            if (InteractionShoot.ProjectilesPool != null)
                InteractionShoot.ProjectilesPool.Update();
            SpheresPool.Update();
            if (!Debug.IsVisible)
            {
                if (Input.IsKeyDown(Keys.KeyPadEnter))
                {
                    SceneManager.LoadScene(typeof(MenuScene));
                }
                if (Input.IsKeyDown(Keys.F8))
                {
                    Settings.ShowInterface = !Settings.ShowInterface;
                }
              
            }

            if (DeathOn)
            {
                if (!Death.IsPlaying)
                {
                    Window.Instance.Quit();
                }
            }


          
            //spacer.Update();
            Chat.Update();
            dustSpawner.Update();
            PanelUI.Update();
            animator.Update();
        }

        public Action OnRenderCenter;
        public override void Render()
        {
           
            DisposalManager.ProcessDisposals();

            skybox.DrawTransparent(localPlayer);
           
            pointLightsPool.Render();

            Vector3 camPos = localPlayer.CameraRelativeRender ? Vector3.Zero : localPlayer.Position;


            if (InteractionShoot.ProjectilesPool != null)
                InteractionShoot.ProjectilesPool.Render();

            if (InteractionPlaceBlock.lineRenderer != null && Settings.ShowInterface)
                InteractionPlaceBlock.lineRenderer.Render();

            if (InteractionShoot.lineRenderer != null)
                InteractionShoot.lineRenderer.Render();

            localPlayer.Render();
            var node = this as Node3D;
            node.Render();

            world.Render(blockMaterial);
            
            blockDestructionManager.Render();
         
            OnRenderCenter?.Invoke();
            if (InteractionDestroyBlockSurvival.BlockMiningEffect != null)
                InteractionDestroyBlockSurvival.BlockMiningEffect.Render();

         
            //blockSelector.Render(localPlayer);

            SpheresPool.Render();

            PanelUI.DrawItemModel();
            dustSpawner.Render();
        }

        public override void OnGUI()
        {
            HealthColorOverlay.OnGUI();
            CenteredText.OnGUI();
            TagText.OnGUI();
            radarWindow.OnGUI();
            ResourceProcessingGUI.OnGUI();
            CraftingGUI.OnGUI();
            PanelUI.Render();
            localPlayer.OnGUI();
            InventoryUI.OnGUI(localPlayer.Inventory);
            StorageUI.OnGUI();
            CreativeWindowUI.OnGUI();
            Chat.OnGUI();
            ItemControlsUI.OnGUI();
            WelcomeUI.OnGUI();
            PauseUI.OnGUI();

            if (VisualDebug.Enabled)
            {
                WorldTextDebug.OnGUI();
            }

            TagManager.DrawTags(localPlayer, Window.Instance.Size.X, Window.Instance.Size.Y);
            BlackScreenOverlay.OnGUI();
        }

        public override void UnloadContent()
        {
            localPlayer = null;
            PanelUI.Player = null;
            spacer = null;
            
            blockSelector.Dispose();
            TickTaskManager.Dispose();
     
            dustSpawner.Dispose();
            pointLightsPool.Dispose();
            ambient.Dispose();
            Chat.Clear();
            if (InteractionShoot.ProjectilesPool != null)
                InteractionShoot.ProjectilesPool.Dispose();

            ToggleManager.DisableAllWindows();
            blockDestructionManager.Dispose();
            World.DropEffectManager.Dispose();
            TagManager.ClearTags();
            Window.OnResized -= TagManager.OnResized;
            world.Dispose();
            ToggleManager.Dispose();
            Debug.OnVisibilityWasChanged -= OnDebugStateChanged;
            SpheresPool.Dispose();
            InteractionPlaceBlock.lineRenderer = null;
            InteractionDestroyBlockSurvival.BlockMiningEffect = null;
        }
    }
}
