using Engine;
using Engine.Components;
using Engine.GUI;
using Engine.InputPro;
using Engine.Light;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game;
using Spacebox.Game.Commands;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Resource;
using Spacebox.GUI;


namespace Spacebox.Scenes
{

    public struct SpaceSceneArgs
    {
        // (worldName/server, modId, seed, modfolder) + ( modfolderName, key, ip, port, nickname)
        public string worldName;
        public string modId;
        public string seed;
        public string modfolder;
        public string modfolderName;
        public string key;
        public string ip;
        public int port;
        public string nickname;
        public SpaceSceneArgs() { }


    }
    public abstract class BaseSpaceScene : Scene, ISceneWithArgs<SpaceSceneArgs>
    {

        protected Astronaut localPlayer;

        protected BlockMaterial blockMaterial;
        protected SpaceSceneArgs SceneArgs;

        protected BlockSelector blockSelector;
        protected RadarUI radarWindow;

        protected BlockDestructionManager blockDestructionManager;

        protected PointLight pLight;
        private SpheresPool SpheresPool;
        private FreeCamera freeCamera;
        private World world;
        public void Initialize(SpaceSceneArgs param)
        {
            SceneArgs = param;

            SceneAssetsPreloader.Preload(param, this, localPlayer);

            HealthColorOverlay.SetActive(new System.Numerics.Vector3(0, 0, 0), 1);

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

           // InputManager0.AddAction("inputOverlay", Keys.F6);
           /* InputManager0.RegisterCallback("inputOverlay", () =>
            {
                InputOverlay.IsVisible = !InputOverlay.IsVisible;
            });*/

            



        }

        public override void LoadContent()
        {

            var texture = new SpaceTexture(512, 512, World.Seed);

            var mesh = Resources.Load<Engine.Mesh>("Resources/Models/cube.obj");
            var skybox = new Skybox(mesh, texture);
            skybox.Scale = new Vector3(Settings.ViewDistance, Settings.ViewDistance, Settings.ViewDistance);

            Lighting.Skybox = skybox;

            radarWindow = new RadarUI(texture);

            if (localPlayer == null)
            {
                localPlayer = new Astronaut(new Vector3(5, 5, 5));
            }

            PanelUI.Player = localPlayer;


            World.LoadWorldInfo(SceneArgs.worldName);
            blockMaterial = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.LightAtlas, localPlayer);
             world = new World(localPlayer, blockMaterial);
            AttachComponent(world);
            world.Load();
            PlayerSaveLoadManager.LoadPlayer(localPlayer, World.Data.WorldFolderPath);

            AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/spaceBackground.ogg")).Audio.Volume = 0.05f;


            var cameraElement = Overlay.GetElementByType(typeof(CameraElement));
            if (cameraElement != null)
            {
                cameraElement.ElementAfter = new AstronautOverlayElement();
            }
            Overlay.AddElement(new AImedBlockElement());

            Input.SetCursorState(CursorState.Grabbed);



            localPlayer.GameMode = World.Data.Info.GameMode;

            PointLightsPool.Instance = new PointLightsPool( 1);


            blockDestructionManager = new BlockDestructionManager();

            if (Settings.Graphics.EffectsEnabled == true)
            localPlayer.AddChild(DustSpawner.CreateDust());

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
            GeneratorUI.Initialize();
            InventoryUI.Player = localPlayer;
            CreativeWindowUI.SetDefaultIcon(slotTex.Handle, localPlayer);

            blockSelector = new BlockSelector();

            AddChild(new Spacer(localPlayer.Position + new Vector3(5, 5, 7)));
            freeCamera = AddChild(new FreeCamera(localPlayer.Position));
            freeCamera.FOV = localPlayer.FOV;
            freeCamera.DepthFar = localPlayer.DepthFar;
            Camera.Main = localPlayer;
           
            WelcomeUI.OnPlayerSpawned(World.Data.Info.ShowWelcomeWindow);
            WelcomeUI.Init();
            PauseUI.Init();
            SpheresPool = new SpheresPool();


            AddChild(new DirectionalLight());

            GameTime.Init();
        }

        public override void Start()
        {
            base.Start();

            Input.HideCursor();
            BlackScreenOverlay.IsEnabled = false;

            Time.OnTick += () =>
            {
                TickTaskManager.UpdateTasks();
            };
            CraftingGUI.Init();

            Debug.OnVisibilityWasChanged += OnDebugStateChanged;
            Window.OnResized += TagManager.OnResized;


            pLight = PointLightsPool.Instance.Take();
            pLight.Enabled = false;

            Chat.Write("Welcome to Spacebox!", Color4.Yellow);

           // Debug.Log(World.CurrentSector.WorldToLocalPosition(Camera.Main.Position));

        }

        private void OnDebugStateChanged(bool state)
        {
            ToggleManager.SetState("mouse", state);
            ToggleManager.SetState("player", !state);
            ToggleManager.SetState("panel", !state);
            InputManager0.Enabled = !state;
            ToggleManager.DisableAllWindows();
            ToggleManager.SetState("radar", false);
            ToggleManager.SetState("inventory", false);
            Input.MoveCursorToCenter();

            if (Chat.IsVisible)
            {
                Chat.IsVisible = false;
            }
        }

        public override void Update()
        {
            Time.HandleTicks();

           // localPlayer.Update();
            
            base.Update();
           // world.OnUpdate();

            blockDestructionManager.Update();
            MainThreadDispatcher.Instance.ExecutePending();

            if (Input.IsKeyDown(Keys.R))
            {
                RenderSpace.SwitchSpace();

            }
            if (Input.IsKeyDown(Keys.C))
            {
                if (localPlayer.IsMain)
                {
                    Camera.Main = freeCamera;
                }
                else
                {
                    Camera.Main = localPlayer;
                }

            }


            if (Input.IsActionDown("pause"))
            {
                if (Chat.IsVisible && Chat.FocusInput)
                {
                    return;
                }

                int opened = ToggleManager.OpenedWindowsCount;

                ToggleManager.DisableAllWindows();

                if (opened > 0)
                {
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
                    SceneManager.Load<MenuScene>();
                }
                if (Input.IsKeyDown(Keys.F8))
                {
                    Settings.ShowInterface = !Settings.ShowInterface;
                }

            }

            Chat.Update();
            PanelUI.Update();
        }

        public Action OnRenderCenter;
        public override void Render()
        {
            base.Render();

            //localPlayer.Render();
            //world.OnRender();
            DisposalManager.ProcessDisposals();

            PointLightsPool.Instance.Render();


            if (InteractionShoot.ProjectilesPool != null)
                InteractionShoot.ProjectilesPool.Render();

            if (InteractionPlaceBlock.lineRenderer != null && Settings.ShowInterface)
                InteractionPlaceBlock.lineRenderer.Render();

            if (InteractionShoot.lineRenderer != null)
                InteractionShoot.lineRenderer.Render();

            blockDestructionManager.Render();

            OnRenderCenter?.Invoke();
            if (InteractionDestroyBlockSurvival.BlockMiningEffect != null)
                InteractionDestroyBlockSurvival.BlockMiningEffect.Render();


            blockSelector.Render();

            SpheresPool.Render();

            PanelUI.DrawItemModel();
        }

        public override void OnGUI()
        {
        
            HealthColorOverlay.OnGUI();
            CenteredText.OnGUI();

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
            GeneratorUI.OnGUI();


            if (VisualDebug.Enabled)
            {
                WorldTextDebug.OnGUI();
            }

            TagManager.DrawTags(Window.Instance.Size.X, Window.Instance.Size.Y);
            BlackScreenOverlay.OnGUI();
            CenteredText.Hide();

        }

        public override void UnloadContent()
        {
            PanelUI.Player = null;

            blockSelector.Dispose();
            TickTaskManager.Dispose();

            PointLightsPool.Instance.Dispose();
            Chat.Clear();
            if (InteractionShoot.ProjectilesPool != null)
                InteractionShoot.ProjectilesPool.Dispose();
            ToggleManager.DisableAllWindows();
            blockDestructionManager.Dispose();

            TagManager.ClearTags();
            Window.OnResized -= TagManager.OnResized;
            PauseUI.Dispose();
            CraftingGUI.Dispose();
            WelcomeUI.Dispose();
            ToggleManager.Dispose();
            Debug.OnVisibilityWasChanged -= OnDebugStateChanged;
            SpheresPool.Dispose();
            InteractionPlaceBlock.lineRenderer = null;
            InteractionDestroyBlockSurvival.BlockMiningEffect = null;

            GameTime.Dispose();
        }


    }
}
