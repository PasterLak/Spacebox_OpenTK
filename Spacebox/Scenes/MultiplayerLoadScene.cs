using Client;
using Engine;
using Engine.Light;
using Engine.SceneManagement;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.GUI;

namespace Spacebox.Scenes
{
    public class MultiplayerLoadScene : Scene, ISceneWithArgs<SpaceSceneArgs>
    {
        private enum LoadState
        {
            Initializing,
            Connecting,
            WaitingForServerInfo,
            CheckingMods,
            DownloadingMods,
            Finalizing,
            Error
        }

        private SpaceSceneArgs _sceneArgs;
        private LoadState _currentState;
        private float _stateTimer;
        private float _errorReturnTimer;
        private string _errorMessage;
        private bool _downloadRequested;

        private const float ConnectionTimeout = 10.0f;
        private const float GeneralTimeout = 100.0f;
        private const float ErrorDisplayTime = 5.0f;

        private Camera _camera;

        public void Initialize(SpaceSceneArgs param)
        {
            _sceneArgs = param;
            CenteredText.Show();
            SetState(LoadState.Initializing);
            Debug.Log($"[MultiplayerLoad] Target: {param.hostIp}:{param.port}");
        }

        public override void LoadContent()
        {
            _camera = new CameraStatic(Vector3.Zero);
            AddChild(_camera);
            _camera.FOV = 100;

            var skyboxTexture = new SpaceTexture(512, 512, 420);
            Lighting.Skybox = new Skybox(skyboxTexture);

            StartConnection();
        }

        public override void Start()
        {
            base.Start();
            ColorOverlay.FadeOut(new System.Numerics.Vector3(0, 0, 0), 1);
        }

        private void StartConnection()
        {
            try
            {
                if (ClientNetwork.Instance != null)
                {
                    ClientNetwork.Instance.Disconnect("Reconnecting");
                    ClientNetwork.Instance = null;
                }

                var client = new ClientNetwork(_sceneArgs);
                ClientNetwork.Instance = client;

                client.OnServerInfoReceived += OnServerInfoReceived;
                client.OnZipDownloadStart += OnZipDownloadStart;
                client.OnZipDownloadComplete += OnZipDownloadComplete;

                SetState(LoadState.Connecting);
            }
            catch (Exception ex)
            {
                HandleError($"Initialization failed: {ex.Message}");
            }
        }

        public override void Update()
        {
            base.Update();

            if (_currentState != LoadState.Error && _currentState != LoadState.Initializing)
            {
                ClientNetwork.Instance?.PollEvents();
            }

            _stateTimer += Time.Delta;

            switch (_currentState)
            {
                case LoadState.Connecting:
                    UpdateConnecting();
                    break;
                case LoadState.WaitingForServerInfo:
                    UpdateWaitingForInfo();
                    break;
                case LoadState.CheckingMods:
                    UpdateCheckingMods();
                    break;
                case LoadState.DownloadingMods:
                    UpdateDownloading();
                    break;
                case LoadState.Finalizing:
                    UpdateFinalizing();
                    break;
                case LoadState.Error:
                    UpdateError();
                    break;
            }
        }

        private void UpdateConnecting()
        {
            if (ClientNetwork.Instance.IsConnected)
            {
                SetState(LoadState.WaitingForServerInfo);
                return;
            }

            if (ClientNetwork.Instance.NameInUse)
            {
                HandleError("Nickname is already in use.");
                return;
            }

            if (_stateTimer > ConnectionTimeout)
            {
                HandleError("Connection timed out.");
            }
        }

        private void UpdateWaitingForInfo()
        {
            if (ClientNetwork.Instance.ServerInfoReceived)
            {
                SetState(LoadState.CheckingMods);
                return;
            }

            if (!ClientNetwork.Instance.IsConnected)
            {
                HandleError("Disconnected from server.");
                return;
            }

            if (_stateTimer > GeneralTimeout)
            {
                HandleError("Timed out waiting for server info.");
            }
        }

        private void UpdateCheckingMods()
        {
            var info = ClientNetwork.Instance.ReceivedServerInfo;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string globalModPath = Path.Combine(baseDir, "GameSets", info.ModFolderName);

            if (Directory.Exists(globalModPath))
            {
                string localHash = HashHelper.CalculateFolderHash(globalModPath);
                if (localHash == info.ModFolderHash)
                {
                    Debug.Success($"[Mods] Found valid mod in GameSets: {globalModPath}");
                    ClientNetwork.Instance.InvokeZipComplete();
                    SetState(LoadState.Finalizing);
                    return;
                }
                else
                {
                    Debug.Warning($"[Mods] Hash mismatch in GameSets. Local: {localHash} Server: {info.ModFolderHash}");
                }
            }

            string serverModPath = Path.Combine(baseDir, "Server", info.Name, "GameSet", info.ModFolderName);

            if (Directory.Exists(serverModPath))
            {
                string localHash = HashHelper.CalculateFolderHash(serverModPath);
                if (localHash == info.ModFolderHash)
                {
                    Debug.Success($"[Mods] Found valid mod in Server cache: {serverModPath}");
                    _sceneArgs.SearchForGameSetInLocalFolder = false;
                    ClientNetwork.Instance.InvokeZipComplete();
                    SetState(LoadState.Finalizing);
                    return;
                }
                else
                {
                    Debug.Warning($"[Mods] Hash mismatch in Server cache. Local: {localHash} Server: {info.ModFolderHash}");
                }
            }

            Debug.Log("[Mods] No valid local mods found. Downloading...");
            SetState(LoadState.DownloadingMods);
        }

        private void UpdateDownloading()
        {
            if (!_downloadRequested)
            {
                Debug.Log("[Mods] Requesting zip from server...");
                ClientNetwork.Instance.SendZipRequest();
                _downloadRequested = true;
            }

            if (ClientNetwork.Instance.ZipDownloaded)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var info = ClientNetwork.Instance.ReceivedServerInfo;
                string serverModPath = Path.Combine(baseDir, "Server", info.Name, "GameSet", info.ModFolderName);

                _sceneArgs.SearchForGameSetInLocalFolder = false;
                SetState(LoadState.Finalizing);
                return;
            }

            if (!ClientNetwork.Instance.IsConnected)
            {
                HandleError("Disconnected during download.");
                return;
            }
        }

        private void UpdateFinalizing()
        {
            var serverInfo = ClientNetwork.Instance.ReceivedServerInfo;

            _sceneArgs.worldName = serverInfo.Name;
            _sceneArgs.modfolderName = serverInfo.ModFolderName;

            Debug.Success($"[MultiplayerLoad] Ready to launch! Server: {serverInfo.Name}");
            SceneManager.Load<MultiplayerScene, SpaceSceneArgs>(_sceneArgs);
        }

        private void UpdateError()
        {
            _errorReturnTimer -= Time.Delta;
            CenteredText.SetText($"{_errorMessage}\nReturning to menu in {_errorReturnTimer:F1}...");

            if (_errorReturnTimer <= 0)
            {
                ClientNetwork.Instance?.Disconnect("Load failed");
                ClientNetwork.Instance = null;
                SceneManager.Load<MenuScene>();
            }
        }

        private void SetState(LoadState newState)
        {
            _currentState = newState;
            _stateTimer = 0f;
            _downloadRequested = false;

            switch (newState)
            {
                case LoadState.Initializing:
                    CenteredText.SetText("Initializing network...");
                    break;
                case LoadState.Connecting:
                    CenteredText.SetText($"Connecting to {_sceneArgs.hostIp}...");
                    break;
                case LoadState.WaitingForServerInfo:
                    CenteredText.SetText("Handshaking...");
                    break;
                case LoadState.CheckingMods:
                    CenteredText.SetText("Checking game files...");
                    break;
                case LoadState.DownloadingMods:
                    CenteredText.SetText("Downloading server mods...");
                    break;
                case LoadState.Finalizing:
                    CenteredText.SetText("Starting game...");
                    break;
            }
        }

        private void HandleError(string message)
        {
            _errorMessage = message;
            _errorReturnTimer = ErrorDisplayTime;
            _currentState = LoadState.Error;

            Debug.Error($"[MultiplayerLoad] Error: {message}");
        }

        private void OnServerInfoReceived()
        {
            var info = ClientNetwork.Instance.ReceivedServerInfo;
            Debug.Log($"Server Info: {info.Name} | Players: {info.MaxPlayers}");
        }

        private void OnZipDownloadStart()
        {
            if (_currentState == LoadState.DownloadingMods)
                CenteredText.SetText("Downloading server mods (Zip)...");
        }

        private void OnZipDownloadComplete()
        {
            Debug.Success("Mods ready.");
        }

        public override void OnGUI()
        {
            //Theme.ApplySpaceboxTheme();
            
          // CenteredText.OnGUI();
        }

        public override void UnloadContent()
        {
            CenteredText.Hide();
            if (ClientNetwork.Instance != null)
            {
                ClientNetwork.Instance.OnServerInfoReceived -= OnServerInfoReceived;
                ClientNetwork.Instance.OnZipDownloadStart -= OnZipDownloadStart;
                ClientNetwork.Instance.OnZipDownloadComplete -= OnZipDownloadComplete;
            }
        }
    }
}