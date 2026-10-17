using Engine;
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
using Spacebox.Game.Player;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Resource;
using Spacebox.GUI;
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
        var texture = new SpaceTexture(512, 512, World.Seed);

        var skybox = new Skybox(texture);
        skybox.Scale = new Vector3(Settings.ViewDistance, Settings.ViewDistance, Settings.ViewDistance);

        Lighting.Skybox = skybox;

        radarWindow = new RadarUI(texture);

        if (localPlayer == null)
        {
            localPlayer = new LocalAstronaut(new Vector3(5, 5, 5));
        }

        AddChild(new BlockSelector());
        AddChild(localPlayer);

        localPlayer.SetSkinColor(SceneArgs.skinColor);
        PanelUI.Player = localPlayer;

        World.LoadWorldInfo(SceneArgs.worldName);
        blockMaterial = new BlockMaterial(GameAssets.BlocksTexture, GameAssets.EmissionBlocks);
        var world = new World(localPlayer, blockMaterial);
        AttachComponent(world);
        world.Load();
        PlayerSaveLoadManager.LoadPlayer(localPlayer, World.WorldData.WorldFolderPath);

        AttachComponent(new BackgroundMusicComponent("Resources/Audio/Music/spaceBackground.ogg")).Audio.Volume = 0.05f;

        var cameraElement = Overlay.GetElementByType(typeof(CameraElement));
        if (cameraElement != null)
        {
            cameraElement.ElementAfter = new AstronautOverlayElement();
        }
        Overlay.AddElement(new AImedBlockElement());

        Input.SetCursorState(CursorState.Grabbed);

        localPlayer.GameMode = World.WorldData.Info.GameMode;

        if (Settings.Graphics.EffectsEnabled == true)
            localPlayer.AddChild(new DustSpawner());

        Debug.RegisterCommand(new DebugTexturesCommand());
        Debug.RegisterCommand(new TeleportCommand(localPlayer));
        Debug.RegisterCommand(new TeleportLocal(localPlayer));
        Debug.RegisterCommand(new TagCommand(localPlayer));
        Debug.RegisterCommand(new ClearInventoryCommand(localPlayer));
        Debug.RegisterCommand(new GameModCommand(localPlayer));
        Debug.RegisterCommand(new SpawnAroundAsteroidCommand(localPlayer));

        Texture2D slotTex = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/slot.png");
        Texture2D selectedSlotTex = GameAssets.LoadResource<Texture2D>("Resources/Textures/UI/selectedSlot.png");

        slotTex.FilterMode = FilterMode.Nearest;
        slotTex.FlipY();
        selectedSlotTex.FilterMode = FilterMode.Nearest;

        InventoryUI.Initialize(slotTex.Handle);
        StorageUI.Initialize(slotTex.Handle);
        PanelUI.Initialize(localPlayer, slotTex.Handle, selectedSlotTex.Handle);
        GeneratorUI.Initialize();
        InventoryUI.Player = localPlayer;
        CreativeWindowUI.SetDefaultIcon(slotTex.Handle, localPlayer);

        freeCamera = AddChild(new FreeCamera(localPlayer.Position));
        freeCamera.FOV = localPlayer.FOV;
        freeCamera.DepthFar = localPlayer.DepthFar;
        Camera.Main = localPlayer;

        WelcomeUI.OnPlayerSpawned(World.WorldData.Info.ShowWelcomeWindow);
        WelcomeUI.Init();
        PauseUI.Init();
        AttachComponent(new SpheresPool());

        AddChild(new ProjectileHitEffectsManager());
        AddChild(new DirectionalLight());
        AttachComponent(new TagManager());

        TagsSaveLoader.LoadTags(World.WorldData.WorldFolderPath);
    }

    public override void Start()
    {
        base.Start();

        //Input.HideCursor();
        BlackScreenOverlay.IsEnabled = false;

        Time.OnTick += () =>
        {
            TickTaskManager.UpdateTasks();
        };
        CraftingGUI.Init();

        Chat.Write("Welcome to Spacebox!", Color4.Yellow);
    }

    public override void Update()
    {
        Time.HandleTicks();

        base.Update();

        MainThreadDispatcher.Instance.ExecutePending();

#if DEBUG
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
    }

    public Action OnRenderCenter;
    public override void Render()
    {
        base.Render();

        DisposalManager.ProcessDisposals();

        OnRenderCenter?.Invoke();

        PanelUI.DrawItemModel();
    }

    public override void OnGUI()
    {
        base.OnGUI();
        ColorOverlay.OnGUI();

        if(!UIManager.IsUIMode)
            CenteredText.OnGUI();

        radarWindow.OnGUI();
        ResourceProcessingGUI.OnGUI();
        CraftingGUI.OnGUI();

        PanelUI.Render();

        InventoryUI.OnGUI(localPlayer.Inventory);
        StorageUI.OnGUI();
        CreativeWindowUI.OnGUI();
        Chat.OnGUI();
        ItemControlsUI.OnGUI();
        WelcomeUI.OnGUI();
        PauseUI.OnGUI();
        GeneratorUI.OnGUI();
        AnalyzerUI.OnGUI();

        if (VisualDebug.Enabled)
        {
            WorldTextDebug.OnGUI();
        }

        BlackScreenOverlay.OnGUI();
        CenteredText.Hide();
    }

    public override void UnloadContent()
    {
        PanelUI.Player = null;

        TickTaskManager.Dispose();
        RadarUI.Instance.Dispose();

        Projectile.PointLightsPool = null;
        Chat.Clear();

        UIManager.CloseAll();

        StorageUI.Dispose();
        PauseUI.Dispose();
        CraftingGUI.Dispose();
        WelcomeUI.Dispose();

        GameAssets.DisposeAll();
        GameTime.Dispose();
    }
}