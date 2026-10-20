using Engine;
using Spacebox.Game.Tutorial;
using Engine.Components;
using Engine.GUI;
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
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Resource;
using Spacebox.Game.Ships;
using Spacebox.GUI;
using Spacebox.Network;
using System;

namespace Spacebox.Scenes;

public struct SpaceSceneArgs
{
    public string worldName;
    public string modId;
    public string seed;

    public string modfolderName;
    public string key;
    public string hostIp;
    public int port;
    public string nickname;
    public string skinColor = "Red";

    public bool SearchForGameSetInLocalFolder = true;
    public string gameSetServerFolder = "";
    public SpaceSceneArgs() { }

    public override string ToString()
    {
        string s = $"WorldName: {worldName}, ModId: {modId}, Seed: {seed},ModfolderName: {modfolderName}, " +
            $"Key: {key}, HostIp: {hostIp}, Port: {port}, Nickname: {nickname}";
        return s;
    }
}

public abstract class BaseSpaceScene : Scene, ISceneWithArgs<SpaceSceneArgs>
{
    protected LocalAstronaut localPlayer;
    protected BlockMaterial blockMaterial;
    protected SpaceSceneArgs SceneArgs;
    protected RadarUI radarWindow;

    private FreeCamera freeCamera;
    private NetworkSession _networkSession;
    

    public void Initialize(SpaceSceneArgs param)
    {
        SceneArgs = param;

        SceneAssetsPreloader.Preload(param, this, localPlayer);

        ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 0, 0), 1);

        BlackScreenOverlay.IsEnabled = true;
        BlackScreenOverlay.OnGUI();
    }

    public override void LoadContent()
    {
        GameTime.Init();
        if (!Application.IsHeadless) CreateSky();

        if (localPlayer == null)
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
        }

        AddChild(new BlockSelector());
        AddChild(localPlayer);

        localPlayer.SetSkinColor(SceneArgs.skinColor);
        PanelUI.Player = localPlayer;

        if (GameSession.IsRemoteClient && GameSession.RemoteWorld != null)
            World.LoadRemoteWorldInfo(GameSession.RemoteWorld);
        else
            World.LoadWorldInfo(SceneArgs.worldName);
        blockMaterial = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);
        var world = new World(localPlayer, blockMaterial);
        AttachComponent(world);
        world.Load();
        if (GameSession.IsRemoteClient)
            PlayerSaveLoadManager.LoadPlayerFromJson(localPlayer, GameSession.PendingPlayerJson);
        else
            PlayerSaveLoadManager.LoadPlayer(localPlayer, World.WorldData.WorldFolderPath);

        if (!Application.IsHeadless)
        {
            var background = AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/spaceBackground.ogg"));
            background.Audio.Volume = 0.05f;
            background.Audio.Channel = Engine.Audio.AudioChannel.Ambient;
        }

        var cameraElement = Overlay.GetElementByType(typeof(CameraElement));
        if (cameraElement != null)
        {
            cameraElement.ElementAfter = new AstronautOverlayElement();
        }
        Overlay.AddElement(new AImedBlockElement());

        Input.SetCursorState(CursorState.Grabbed);

        localPlayer.GameMode = World.WorldData.Info.GameMode;
        TutorialService.Attach(localPlayer, World.WorldData.Info.ShowWelcomeWindow && !GameSession.IsRemoteClient);

        if (!Application.IsHeadless) localPlayer.AddChild(new DustSpawner());

        Debug.RegisterCommand(new DebugTexturesCommand());
        Debug.RegisterCommand(new TeleportCommand(localPlayer));
        Debug.RegisterCommand(new TeleportLocal(localPlayer));
        Debug.RegisterCommand(new TagCommand(localPlayer));
        Debug.RegisterCommand(new ClearInventoryCommand(localPlayer));
        Debug.RegisterCommand(new GameModCommand(localPlayer));
        Debug.RegisterCommand(new SpawnAroundAsteroidCommand(localPlayer));
        Debug.RegisterCommand(new TeleportToPlayerCommand(localPlayer));
        Debug.RegisterCommand(new SpawnMobCommand(localPlayer));
        Debug.RegisterCommand(new SsaoDebugCommand());

        InventoryUI.Initialize();
        StorageUI.Initialize();
        PanelUI.Initialize(localPlayer);
        GeneratorUI.Initialize();
        InventoryUI.Player = localPlayer;
        CreativeWindowUI.SetDefaultIcon(localPlayer);

        freeCamera = AddChild(new FreeCamera(localPlayer.Position));
        freeCamera.FOV = localPlayer.FOV;
        freeCamera.DepthFar = localPlayer.DepthFar;
        Camera.Main = localPlayer;

        world.PreloadAroundPlayer();
        WorldLoadingScreen.Begin(localPlayer);

        WelcomeUI.OnPlayerSpawned(World.WorldData.Info.ShowWelcomeWindow);
        WelcomeUI.Init();
        Spacebox.Game.Tutorial.UI.TutorialUI.Init();
        PauseUI.Init();
        AttachComponent(new SpheresPool());

        AddChild(new ProjectileHitEffectsManager());
        AddChild(new DirectionalLight());
        AttachComponent(new TagManager()).RadarSource = () => localPlayer.Radar;
        AttachComponent(new MobSystem());

        TagsSaveLoader.LoadTags(World.WorldData.WorldFolderPath);

        if (GameSession.IsDedicated)
        {
            localPlayer.IsFrozen = true;
            localPlayer.Flashlight.Enabled = false;
        }

        ConnectionScreen.Hide();
        _networkSession = new NetworkSession(this, localPlayer);
        if (!_networkSession.Start() && GameSession.IsRemoteClient)
        {
            ConnectionScreen.ShowMessage("Connection failed", "Lost the connection to the server while loading");
            SceneManager.Load<MenuScene>();
        }
    }

    private void CreateSky()
    {
        var texture = new SpaceTexture(512, 512, World.Seed);

        var skybox = new Skybox(texture);
        skybox.Scale = new Vector3(Settings.ViewDistance, Settings.ViewDistance, Settings.ViewDistance);

        Lighting.Skybox = skybox;

        radarWindow = new RadarUI(texture);
    }

    public override void Start()
    {
        base.Start();

        //Input.HideCursor();
        BlackScreenOverlay.IsEnabled = false;

        Time.OnTick += UpdateTickTasks;
        CraftingGUI.Init();

        Chat.Write("Welcome to Spacebox!", Color4.Yellow);
    }

    public override void Update()
    {
        Time.HandleTicks();

        base.Update();

        MainThreadDispatcher.Instance.ExecutePending(StreamingBudget.DispatcherBudgetMs);
        _networkSession?.Update();
        TutorialService.Update(Time.Delta);
        WorldLoadingScreen.Update();
        DnaReconstructionScreen.Update();
        Spacebox.Game.Rendering.CameraFeedController.Update();
        Spacebox.Game.Rendering.VignetteController.SetWarning(Spacebox.Game.Player.OxygenWarning.For(localPlayer));

#if DEBUG
        if (Input.IsKeyDown(Keys.R))
        {
            RenderSpace.Toggle();
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
#endif

        if (Input.IsKeyDown(Keys.Escape))
        {
            if (UIManager.IsTop("chat") && Chat.FocusInput)
            {
                return;
            }

            if (UIManager.IsUIMode)
            {
                UIManager.CloseTop();
            }
            else
            {
                UIManager.Open("pause");
            }
        }

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
        Spacebox.Game.Tutorial.UI.TutorialUI.Update();
        InputOverlay.Update();
        Spacebox.Game.Waypoints.WaypointWindowUI.Update();
    }

    public Action OnRenderCenter;
    public override void Render()
    {
        base.Render();

        DisposalManager.ProcessDisposals();

        OnRenderCenter?.Invoke();

        if (!WorldLoadingScreen.IsActive)
        {
            PanelUI.DrawItemModel();
            EquipmentUI.RenderPreview();
        }
    }

    public override void OnGUI()
    {
        if (WorldLoadingScreen.IsActive)
        {
            WorldLoadingScreen.OnGUI();
            return;
        }

        base.OnGUI();
        ColorOverlay.OnGUI();

        if(!UIManager.IsUIMode)
            CenteredText.OnGUI();

        radarWindow?.OnGUI();
        ResourceProcessingGUI.OnGUI();
        CraftingGUI.OnGUI();

        PanelUI.Render();

        InventoryUI.OnGUI(localPlayer.Inventory);
        EquipmentUI.OnGUI(localPlayer);
        StorageUI.OnGUI();
        CreativeWindowUI.OnGUI();
        Chat.OnGUI();
        ItemControlsUI.OnGUI();
        Spacebox.Game.Tutorial.UI.TutorialUI.OnGUI();
        WelcomeUI.OnGUI();
        PauseUI.OnGUI();
        Spacebox.Game.Tutorial.UI.TutorialPromptUI.OnGUI();
        Spacebox.Game.Waypoints.WaypointWindowUI.OnGUI();
        Spacebox.Game.GUI.CameraFeedOverlayUI.OnGUI();
        Spacebox.Game.GUI.FpsCounterUI.OnGUI();
        if (Settings.ShowInterface) InputOverlay.OnGUI();
        GeneratorUI.OnGUI();
        AnalyzerUI.OnGUI();
        OxygenGeneratorUI.OnGUI();
        DisassemblerUI.OnGUI();
        GravityGeneratorUI.OnGUI();
        ShipConsoleUI.OnGUI();
        DnaReconstructorUI.OnGUI();
        DnaReconstructionScreen.OnGUI();
        HitMarkerUI.OnGUI();
        if (!UIManager.IsUIMode) RepairTargetUI.OnGUI();

        if (VisualDebug.Enabled)
        {
            WorldTextDebug.OnGUI();
        }

        BlackScreenOverlay.OnGUI();
        CenteredText.Hide();
    }

    private static void UpdateTickTasks()
    {
        TickTaskManager.UpdateTasks();
    }

    public override void UnloadContent()
    {
        _networkSession?.Dispose();
        _networkSession = null;
        Time.OnTick -= UpdateTickTasks;
        MainThreadDispatcher.Instance.Clear();
        PerlinWorms.ClearCache();
        ResourceProcessingGUI.Reset();
        OxygenGeneratorUI.Reset();
        DisassemblerUI.Reset();
        WorldLoadingScreen.Reset();
        GravityGeneratorUI.Reset();
        DnaReconstructorUI.Reset();
        DnaReconstructionScreen.Reset();
        HitMarkerUI.Reset();
        RepairTargetUI.Reset();
        EquipmentUI.Reset();
        ShipConsoleUI.Reset();
        ShipPilot.Reset();
        Spacebox.Game.Rendering.CameraFeedController.Reset();
        Spacebox.Game.Rendering.VignetteController.SetWarning(0f);
        ShipRider.Reset();
        ShipNetwork.Reset();
        ShipWeapons.Reset();
        TutorialService.Detach();
        Spacebox.Game.Waypoints.WaypointWindowUI.Reset();
        PanelUI.Player = null;

        TickTaskManager.Dispose();
        radarWindow?.Dispose();

        Projectile.PointLightsPool = null;
        Chat.Clear();

        UIManager.CloseAll();

        StorageUI.Dispose();
        PauseUI.Dispose();
        CraftingGUI.Dispose();
        WelcomeUI.Dispose();
        Spacebox.Game.Tutorial.UI.TutorialUI.Dispose();

        GameAssets.DisposeAll();
        GameTime.Dispose();
    }
}