using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Client;
using DryIoc;
using Engine;
using Engine.SceneManagment;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Client;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.GUI;

using static Spacebox.Game.Resources.GameSetLoader;

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
        public MultiplayerLoadScene(string[] args) : base(args)
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
            player = new CameraBasic(new Vector3(0, 0, 0));

            skyboxShader = ShaderManager.GetShader("Shaders/skybox");
            skybox = new Skybox("Resources/Models/cube.obj", skyboxShader,
                new SpaceTexture(512, 512, new Random()));
            skybox.IsAmbientAffected = false;

            Debug.Warning("Trying to connect to server...");
            CenteredText.SetText("Trying to connect to server...");

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

        public override void Start()
        {
           
        }
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
                    var world = new WorldInfo { Name = sceneArgs[0], ModId = sceneArgs[1], Seed = sceneArgs[2], FolderName = sceneArgs[3] };
                    var modConfig = new ModConfig { ModId = sceneArgs[1], FolderName = sceneArgs[3] };
                    var serverInfo = new ServerInfo();
                    serverInfo.Name = sceneArgs[0];
                    serverInfo.Port = port;
                    serverInfo.IP = host;


                    WriteInfo($"Connected to: {serverInfo.Name} host: {host} port: {port}");

                    //SceneLauncher.LaunchMultiplayerGame(world, modConfig, serverInfo, playerName, appKey);
                   
                    SceneManager.LoadScene(typeof(MultiplayerScene), sceneArgs);
                }
                else
                {
                    if (error == false)
                    {
                        WriteError("Connection error: " + connectionError);
                        timeToGoToMenu = 3;
                        error = true;
                    }

                    timeToGoToMenu -= Time.Delta;

                    if(timeToGoToMenu < 0)
                    {
                        WriteError("Returning to Multiplayer Menu.");
                        ClientNetwork.Instance = null;
                        SceneManager.LoadScene(typeof(MenuScene));

                    }

                }
            }
        }

        public override void Render()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            skybox.DrawTransparent(player);
           // spawner.Render();
        }

        public override void OnGUI()
        {
            CenteredText.Draw();
        }

        public override void UnloadContent()
        {
            CenteredText.Hide();

            skybox.Texture.Dispose();

            skyboxShader.Dispose();
           
        }
    }
}
