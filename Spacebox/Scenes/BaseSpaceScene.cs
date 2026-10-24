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
    public string[] extensions = Array.Empty<string>();
    public string[] serverTexturePacks = Array.Empty<string>();
    public bool applyTexturePacks = true;
    public IReadOnlyList<Spacebox.Game.Resource.Content.ITextureOverrideSource> textureOverrides = null;
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
    protected GameUi ui;
    protected GameScene scene;

    private FreeCamera freeCamera;
    private NetworkSession _networkSession;
    private Spacebox.Game.Rendering.FlashlightBeamRenderer _flashlightBeams;
    private Spacebox.Game.Rendering.BodyOutlineRenderer _bodyOutlines;
    

    private const float WorldRevealFade = 1f;

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
        scene = new GameScene();
        scene.MakeCurrent();
        ui = new GameUi();
        ui.MakeCurrent();
        GameTime.Init();
        if (!Application.IsHeadless) CreateSky();

        if (localPlayer == null)
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
        }

        AddChild(new BlockSelector());
        AddChild(Spacebox.Game.Ships.Sharing.ShipPlacementMode.CreatePreview());
        AddChild(localPlayer);

        localPlayer.SetSkinColor(SceneArgs.skinColor);
        ui.Panel.Player = localPlayer;

        if (GameSession.IsRemoteClient && GameSession.RemoteWorld != null)
            World.LoadRemoteWorldInfo(GameSession.RemoteWorld);
        else
            World.LoadWorldInfo(SceneArgs.worldName);
        Spacebox.Game.Protection.ClaimRegistry.Load(World.WorldData.WorldFolderPath);
        if (GameSession.IsRemoteClient) Spacebox.Network.Players.PlayerGameModes.Current.Reset();
        else Spacebox.Network.Players.PlayerGameModes.Current.Load(World.WorldData.WorldFolderPath);
        if (!GameSession.IsRemoteClient && !GameSession.IsDedicated) scene.WorldRules.Load(World.WorldData.WorldFolderPath);
        if (!GameSession.IsRemoteClient) scene.Skins.Load(World.WorldData.WorldFolderPath);
        blockMaterial = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);
        var world = new World(localPlayer, blockMaterial);
        AttachComponent(world);
        world.Load();
        localPlayer.GameMode = World.WorldData.Info.GameMode;
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
        if (!Application.IsHeadless) _flashlightBeams = new Spacebox.Game.Rendering.FlashlightBeamRenderer();
        if (!Application.IsHeadless) _bodyOutlines = new Spacebox.Game.Rendering.BodyOutlineRenderer();

        Debug.RegisterCommand(new DebugTexturesCommand());
        Debug.RegisterCommand(new Spacebox.Game.Commands.BackupCommand());
        Debug.RegisterCommand(new Spacebox.Game.Commands.UseBlockCommand(localPlayer));
        Debug.RegisterCommand(new Spacebox.Game.Commands.Teleport.TeleportCommand(localPlayer));
        Debug.RegisterCommand(new Spacebox.Game.Commands.Teleport.TeleportCommand(localPlayer, "teleport"));
        Debug.RegisterCommand(new Spacebox.Game.Commands.Teleport.TeleportCommand(localPlayer, "tpp"));
        Debug.RegisterCommand(new Spacebox.Game.Commands.Teleport.TeleportHereCommand(localPlayer));
        Debug.RegisterCommand(new TeleportLocal(localPlayer));
        Debug.RegisterCommand(new TagCommand(localPlayer));
        Debug.RegisterCommand(new ClearInventoryCommand(localPlayer));
        Debug.RegisterCommand(new GameModCommand(localPlayer));
        Debug.RegisterCommand(new SpawnAroundAsteroidCommand(localPlayer));
        Debug.RegisterCommand(new SpawnMobCommand(localPlayer));
        Debug.RegisterCommand(new SsaoDebugCommand());
        Debug.RegisterCommand(new OcclusionCommand());
        Debug.RegisterCommand(new GameRuleCommand());
        Debug.RegisterCommand(new GiveCommand(localPlayer));
        Debug.RegisterCommand(new StatCommand(localPlayer, "heal", PlayerStat.All));
        Debug.RegisterCommand(new KillCommand(localPlayer));
        Debug.RegisterCommand(new StatCommand(localPlayer, "health", PlayerStat.Health));
        Debug.RegisterCommand(new StatCommand(localPlayer, "power", PlayerStat.Power));
        Debug.RegisterCommand(new StatCommand(localPlayer, "air", PlayerStat.Air));
        Debug.RegisterCommand(new SetStatCommand(localPlayer));
        Debug.RegisterCommand(new HitCommand(localPlayer));
        Debug.RegisterCommand(new SkinCommand(localPlayer));
        Debug.RegisterCommand(new ItemsCommand());
        Debug.RegisterCommand(new SeedCommand());
        Debug.RegisterCommand(new PositionCommand(localPlayer));
        Debug.RegisterCommand(new SetSpawnCommand(localPlayer));
        Debug.RegisterCommand(new SpawnCommand(localPlayer));
        Debug.RegisterCommand(new ProtectCommand(localPlayer, true));
        Debug.RegisterCommand(new ProtectCommand(localPlayer, false));
        Debug.RegisterCommand(new ProtectedListCommand(localPlayer));

        Spacebox.Game.GUI.Sounds.UiSounds.Initialize();
        _gameCanvas = new Spacebox.Game.GUI.Canvas.GameCanvas(ui);
        ui.Initialize(localPlayer);

        freeCamera = AddChild(new FreeCamera(localPlayer.Position));
        freeCamera.FOV = localPlayer.FOV;
        freeCamera.DepthFar = localPlayer.DepthFar;
        Camera.Main = localPlayer;

        world.PreloadAroundPlayer();
        ui.Loading.Begin(localPlayer, () =>
        {
            PlayerSaveLoadManager.SettleSpawn(localPlayer);
            ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 0, 0), 1f, WorldRevealFade);
        });

        ui.Welcome.OnPlayerSpawned(World.WorldData.Info.ShowWelcomeWindow);
        AttachComponent(new SpheresPool());

        AddChild(new ProjectileHitEffectsManager());
        AddChild(new DirectionalLight());
        var tags = AttachComponent(new TagManager());
        tags.RadarSource = () => localPlayer.Radar;
        scene.AttachTags(tags);
        AttachComponent(new MobSystem());

        TagsSaveLoader.LoadTags(World.WorldData.WorldFolderPath);
        if (GameSession.IsRemoteClient) Spacebox.Game.Protection.EntityProtection.Reset();
        else Spacebox.Game.Protection.EntityProtection.Load(World.WorldData.WorldFolderPath);

        if (GameSession.IsDedicated)
        {
            localPlayer.IsFrozen = true;
            localPlayer.Flashlight.Enabled = false;
        }

        ConnectionScreen.Hide();
        _networkSession = new NetworkSession(this, localPlayer, scene.Skins);
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
        ui.AttachRadar(radarWindow);
    }

    public override void Start()
    {
        base.Start();

        //Input.HideCursor();
        BlackScreenOverlay.IsEnabled = false;

        Time.OnTick += UpdateTickTasks;

        ui.Chat.Write("Welcome to Spacebox!", Color4.Yellow);
    }

    private Spacebox.Game.GUI.Canvas.GameCanvas _gameCanvas;

    public override void Update()
    {
        Time.HandleTicks();

        base.Update();

        MainThreadDispatcher.Instance.ExecutePending(StreamingBudget.DispatcherBudgetMs);
        _networkSession?.Update();
        TutorialService.Update(Time.Delta);
        ui.Loading.Tick();
        if (!WorldSaveState.IsTracking && !ui.Loading.IsActive) World.Instance?.StartTrackingChanges();
        ui.DnaReconstruction.Tick();
        _gameCanvas?.Update(localPlayer, Time.Delta);
        Spacebox.Game.Rendering.CameraFeedController.Update();
        Spacebox.Game.Rendering.FlashlightGlareController.Update(localPlayer);
        Spacebox.Game.Protection.ClaimRegistry.Update(Time.Delta);
        Spacebox.Game.Rendering.VignetteController.SetWarning(Spacebox.Game.Player.OxygenWarning.For(localPlayer));
        Spacebox.Game.Player.LowOxygenAlarm.Update(localPlayer, Time.Delta, ui.Loading.IsActive);
        scene.MachineSounds.Update(Time.Delta);

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

        if (Input.IsKeyDown(Keys.Escape) && !UIManager.IsTop("chat"))
        {
            if (ui.ShipConsole.IsWaitingForKey)
            {
            }
            else if (Spacebox.Game.Ships.Sharing.ShipPlacementMode.IsActive && !UIManager.IsUIMode)
            {
                Spacebox.Game.Ships.Sharing.ShipPlacementMode.Cancel();
            }
            else if (Spacebox.Game.GUI.Canvas.GameCanvas.HadOpenDialog)
            {
            }
            else if (UIManager.IsUIMode)
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
            if (Input.IsKeyDown(Keys.KeyPadEnter) && !UIManager.IsUIMode)
            {
                ui.Pause.RequestGoToMenu();
            }
            if (Input.IsKeyDown(Keys.F8))
            {
                Settings.ShowInterface = !Settings.ShowInterface;
            }
        }

        ui.Update();
        InputOverlay.Update();
    }

    public Action OnRenderCenter;
    public override void Render()
    {
        if (ui.Loading.IsActive)
        {
            RenderSkybox();
            return;
        }

        base.Render();
        _flashlightBeams?.Render();
        if (Spacebox.Game.Rendering.WorldPreviewCapture.IsActive) return;
        _bodyOutlines?.Render();
        scene.Claims.Update(localPlayer, Time.Delta);
        if (VisualDebug.Enabled)
        {
            VisualDebug.DrawAxes(Vector3.Zero, 10f);
            DebugGizmos.Draw(this);
            Engine.GUI.GizmoIcons.Add(Spacebox.Game.Player.Respawn.WorldSpawn.For(localPlayer), Engine.GUI.GizmoIcon.SpawnPoint, Color4.LightGreen);
        }

        OnRenderCenter?.Invoke();

        if (!ui.Loading.IsActive)
        {
            ui.Panel.DrawItemModel();
            ui.Equipment.RenderPreview();
        }
    }

    public override void OnGUI()
    {
        _gameCanvas?.Render();
        if (ui.Loading.IsActive) return;

        base.OnGUI();
        ColorOverlay.Update();

        ui.OnGUI();

        BlackScreenOverlay.OnGUI();
        CenteredText.Hide();
    }

    private static void UpdateTickTasks()
    {
        TickTaskManager.UpdateTasks();
    }

    public override void UnloadContent()
    {
        _gameCanvas?.Dispose();
        _gameCanvas = null;
        _networkSession?.Dispose();
        _networkSession = null;
        Spacebox.Game.Resource.TexturePacks.TexturePackService.SetSessionPacks(null);
        Time.OnTick -= UpdateTickTasks;
        MainThreadDispatcher.Instance.Clear();
        PerlinWorms.ClearCache();
        Spacebox.Game.Protection.EntityProtection.Reset();
        Spacebox.Game.Protection.ClaimRegistry.Reset();
        Spacebox.Network.Players.PlayerGameModes.Current.Reset();
        Spacebox.Game.Protection.ClaimSettings.ClearOverride();
        Spacebox.Network.PictureFrameNetwork.Reset();
        if (!GameSession.IsDedicated)
        {
            Spacebox.Game.Rules.ServerRules.ResetSession();
            Spacebox.Game.Mobs.MobRules.FriendlyMobs = false;
        }
        Spacebox.Game.Pictures.PictureTextures.Clear();
        scene.Dispose();
        Spacebox.Game.Rendering.CameraFeedController.Reset();
        Spacebox.Game.Rendering.BodyOutlineRenderer.Reset();
        Spacebox.Game.Rendering.FlashlightGlareController.Reset();
        Spacebox.Game.Rendering.VignetteController.SetWarning(0f);
        Spacebox.Game.Generation.Blocks.BlockDamageVisuals.Instance.Reset();
        Spacebox.Game.Generation.Doors.DoorSounds.Clear();
        Spacebox.Game.Effects.BlockEventSounds.Clear();
        Spacebox.Game.Player.LowOxygenAlarm.Reset();
        Spacebox.Game.Player.EquipmentSounds.Clear();
        Spacebox.Game.GUI.Sounds.UiSounds.Dispose();
        Spacebox.Game.Player.AirZoneSounds.Clear();
        Spacebox.Game.Player.Footsteps.FootstepSounds.Clear();
        ShipNetwork.Reset();
        ShipWeapons.Reset();
        TickTaskManager.Dispose();

        Projectile.PointLightsPool = null;

        UIManager.CloseAll();

        TutorialService.Detach();
        ui.Reset();

        GameAssets.DisposeAll();
        GameTime.Dispose();
    }
}