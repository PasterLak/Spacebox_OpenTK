
using Client;
using Engine;
using Engine.SceneManagement;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using Spacebox.Scenes.Test;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Scenes
{
    public class MultiplayerLoadScene : Scene, ISceneWithArgs<SpaceSceneArgs>
    {
        private bool connectionAttempted = false;
        private bool connectionSuccessful = false;
        private string connectionError = "kicked";
        private float elapsedTime = 0f;
        private const float timeout = 100f;
        private SpaceSceneArgs sceneArgs;
        private ClientNetwork networkClient;
        private float timeToGoToMenu = 10f;
        private Camera player;

        private bool readyToLaunch = false;


        public void Initialize(SpaceSceneArgs param)
        {

            this.sceneArgs = param;

            WriteInfo($"Server info: host {param.hostIp} port {param.port} key {param.key} namePlayer {param.nickname}");
            CenteredText.SetText("Loading");
            CenteredText.Show();
        }

        public override void LoadContent()
        {
            player = new CameraStatic(new Vector3(0, 0, 0));

            AddChild(new Skybox(
                new SpaceTexture(512, 512, World.Seed)));

            Debug.Warning("Trying to connect to server...");
            CenteredText.SetText("Trying to connect to server...");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    networkClient = new ClientNetwork(sceneArgs.key, sceneArgs.hostIp, sceneArgs.port, sceneArgs.nickname);
                    if (ClientNetwork.Instance == null)
                        ClientNetwork.Instance = networkClient;
                    networkClient.OnServerInfoReceived += () =>
                    {
                        WriteInfo("Server information received...\n" + networkClient.ReceivedServerInfo.ToString());
                    };
                    networkClient.OnZipDownloadStart += () =>
                    {
                        WriteInfo("Starting loading mods...");
                    };
                    networkClient.OnZipDownloadComplete += () =>
                    {
                        WriteInfo("Mods loaded.");
                    };
                    int attempts = 0;
                    while (!networkClient.IsConnected && attempts < 50)
                    {
                        Thread.Sleep(100);
                        attempts++;
                    }
                    connectionSuccessful = networkClient.IsConnected;
                    connectionAttempted = true;
                }
                catch (Exception ex)
                {
                    connectionError = ex.Message;
                    connectionAttempted = true;
                }
            });
        }
        public void WriteInfo(string text)
        {
            CenteredText.SetText(text);
            Debug.Success(text);
        }
        public void WriteError(string text)
        {
            CenteredText.SetText(text);
            Debug.Error(text);
        }
        public override void Start() { }
        bool error = false;
        public override void Update()
        {
            if (networkClient != null)
                networkClient.PollEvents();
            float delta = Time.Delta;
            elapsedTime += delta;
            if (elapsedTime >= timeout && !connectionAttempted)
            {
                connectionAttempted = true;
                connectionSuccessful = false;
                connectionError = "Connection timed out.";
            }
            if (connectionAttempted)
            {
                if (connectionSuccessful)
                {
                    if (!networkClient.ServerInfoReceived)
                    {
                        CenteredText.SetText("Waiting for server information...");
                        return;
                    }
                    if (!networkClient.ZipDownloaded)
                    {
                        CenteredText.SetText("Loading mods...");
                        return;
                    }
                    if (!readyToLaunch)
                    {
                        readyToLaunch = true;

                        var serverInfo = new SpaceNetwork.ServerInfo
                        {
                            Name = networkClient.ReceivedServerInfo.Name,
                            Description = networkClient.ReceivedServerInfo.Description,
                            MaxPlayers = networkClient.ReceivedServerInfo.MaxPlayers
                        };

                        sceneArgs.worldName = serverInfo.Name;
                        var world = new WorldInfo { Name = serverInfo.Name, ModId = sceneArgs.modId, Seed = sceneArgs.seed, FolderName = sceneArgs.modfolder };
                        var modConfig = new ModConfig { ModId = sceneArgs.modId, FolderName = sceneArgs.modfolder };
                        WriteInfo($"Connected to: <{serverInfo.Name}> host: {sceneArgs.hostIp} port: {sceneArgs.port}");

                        SceneManager.Load<MultiplayerScene, SpaceSceneArgs>(sceneArgs); 
                    }
                }
                else
                {
                    if (!error)
                    {
                        WriteError("Connection error: " + connectionError);
                        timeToGoToMenu = 3;
                        error = true;
                    }
                    timeToGoToMenu -= Time.Delta;
                    if (timeToGoToMenu < 0)
                    {
                        WriteError("Returning to Multiplayer Menu.");
                        ClientNetwork.Instance = null;
                        SceneManager.Load<MenuScene>();
                    }
                }
            }
        }
        public override void Render()
        {


        }
        public override void OnGUI()
        {
            CenteredText.OnGUI();
        }
        public override void UnloadContent()
        {
            //CenteredText.Hide();
            // skybox.Texture.Dispose();

        }


    }
}
