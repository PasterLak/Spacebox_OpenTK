using System;
using System.Threading;
using Client;
using Engine;
using Engine.SceneManagment;
using OpenTK.Mathematics;
using Spacebox.Client;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using Spacebox.Scenes;
using static Spacebox.Game.Resources.GameSetLoader;

namespace Spacebox.Scenes
{
    public class MultiplayerMenuScene : Scene
    {
        private string appKey = "MyAppKey";
        private string host = "127.0.0.1";
        private int port = 14242;
        private string playerName = "PlayerName";
        private bool connectionAttempted = false;
        private bool connectionSuccessful = false;
        private string connectionError = "";
        private float elapsedTime = 0f;
        private const float timeout = 5f;
        private string[] sceneArgs;
        private ClientNetwork networkClient;

        public MultiplayerMenuScene(string[] args) : base(args)
        {
            sceneArgs = args;
        }

        public override void LoadContent()
        {
            Debug.Log("Trying to connect to server...");
        }

        public override void Start()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    networkClient = new ClientNetwork(appKey, host, port, playerName);
                    if (ClientNetwork.Instance == null)
                    {
                        ClientNetwork.Instance = networkClient;
                    }
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

        public override void Update()
        {
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
                    var world = new WorldInfo { Name = sceneArgs[0], ModId = sceneArgs[1], Seed = sceneArgs[2], FolderName = sceneArgs[3] };
                    var modConfig = new ModConfig { ModId = sceneArgs[1], FolderName = sceneArgs[3] };
                    var serverInfo = new ServerInfo();
                    serverInfo.Name = sceneArgs[0];
                    serverInfo.Port = port;
                    serverInfo.IP = host;
                    SceneLauncher.LaunchMultiplayerGame(world, modConfig, serverInfo, playerName, appKey);
                }
                else
                {
                    Debug.Error("Connection error: " + connectionError);
                }
            }
        }

        public override void Render()
        {
        }

        public override void OnGUI()
        {
        }

        public override void UnloadContent()
        {
        }
    }
}
