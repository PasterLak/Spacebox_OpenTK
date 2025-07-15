
using Client;
using Engine;
using Engine.SceneManagement;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.GUI;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Scenes
{
    public class MultiplayerLoadScene : Scene
    {
        private string appKey = "0.1.1";
        private string host = "192.168.56.1";
        private int port = 5544;
        private string playerName = "PlayerName";
        private bool connectionAttempted = false;
        private bool connectionSuccessful = false;
        private string connectionError = "kicked";
        private float elapsedTime = 0f;
        private const float timeout = 100f;
        private string[] sceneArgs;
        private ClientNetwork networkClient;
        private float timeToGoToMenu = 10f;
        private Camera player;
        private Skybox skybox;
        private Shader skyboxShader;
        private bool readyToLaunch = false;


        public MultiplayerLoadScene()
        {

        }
        public MultiplayerLoadScene(string[] args) 
        {
            sceneArgs = args;
            if (args.Length >= 8)
            {
                appKey = args[4];
                host = args[5];
                int.TryParse(args[6], out port);
                playerName = args[7];
            }
            WriteInfo($"Server info: host {host} port {port} key {appKey} namePlayer {playerName}");
            CenteredText.SetText("Loading");
            CenteredText.Show();
        }
        public override void LoadContent()
        {
            player = new CameraStatic(new Vector3(0, 0, 0));

            var mesh = Resources.Load<Engine.Mesh>("Resources/Models/cube.obj");
            skybox = new Skybox(mesh,
                new SpaceTexture(512, 512, World.Seed));
           
            Debug.Warning("Trying to connect to server...");
            CenteredText.SetText("Trying to connect to server...");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    networkClient = new ClientNetwork(appKey, host, port, playerName);
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

                        sceneArgs[0] = serverInfo.Name;
                        var world = new WorldInfo { Name = serverInfo.Name, ModId = sceneArgs[1], Seed = sceneArgs[2], FolderName = sceneArgs[3] };
                        var modConfig = new ModConfig { ModId = sceneArgs[1], FolderName = sceneArgs[3] };
                        WriteInfo($"Connected to: <{serverInfo.Name}> host: {host} port: {port}");

                        /*SceneManager.Load<MultiplayerScene, SpaceSceneArgs>(new SpaceSceneArgs()
                        {
                            worldName = serverInfo.Name,
                            modId = world.ModId,
                            seed = world.Seed,
                            modfolder = world.FolderName,
                            port = port,
                            

                        }); */
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
            GL.Clear(ClearBufferMask.ColorBufferBit);
            skybox.Render();
        }
        public override void OnGUI()
        {
            CenteredText.OnGUI();
        }
        public override void UnloadContent()
        {
            //CenteredText.Hide();
           // skybox.Texture.Dispose();
            skyboxShader.Dispose();
        }
    }
}
