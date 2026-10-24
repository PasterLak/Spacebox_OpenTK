using Engine;
using Engine.Networking.Query;
using Engine.SceneManagement;
using Spacebox.Network;
using Spacebox.Network.Discovery;
using Spacebox.Scenes;
using System.Numerics;

namespace Spacebox.Game.GUI.Menu
{
    public class MultiplayerWindow : MenuWindow, IDisposable
    {
        public GameMenu menu;
        private ClientConfig config;
        private string nickname = "";

        private readonly string[] availableColors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
        private int selectedColorIndex = 0;

        private readonly ServerIconCache _icons = new ServerIconCache();
        private ServerBrowser _browser;

        public ClientConfig Config => config;
        public IReadOnlyList<string> Colors => availableColors;
        public int SelectedColorIndex => selectedColorIndex;
        public string Nickname => nickname;
        public ServerIconCache Icons => _icons;

        public ServerBrowser Browser
        {
            get
            {
                EnsureBrowser();
                return _browser;
            }
        }

        public ServerListItem Selected { get; set; }

        public bool CanEdit(ServerListItem item) => item != null && IsListed(item);

        protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.MultiplayerPage(this);

        public MultiplayerWindow(GameMenu menu)
        {
            this.menu = menu;
        }

        public override void OnWindowChanged()
        {
            base.OnWindowChanged();
            LoadConfig();
            nickname = config.PlayerNickname;

            selectedColorIndex = Array.IndexOf(availableColors, config.SkinColor);
            if (selectedColorIndex == -1)
            {
                selectedColorIndex = 0;
                config.SkinColor = availableColors[0];
            }

            ChangePlayerTexture(config.SkinColor);

            EnsureBrowser();
            _browser.SetSavedServers(config.Servers);
            _browser.KnownLocal = new LocalServerHistory(config.LocalServers);
            _browser.Refresh();
        }

        public ClientConfig LoadConfig()
        {
            config = ClientConfigStore.Load();
            return config;
        }

        public void SaveConfig()
        {
            if (config != null) ClientConfigStore.Save(config);
        }

        public ClientConfig GetConfig() => config;

        public void ChangePlayerTexture(string color)
        {
            var sceneMenu = Scene.Root as MenuScene;
            if (sceneMenu != null)
            {
                sceneMenu.ChangeAstronautColor(color);
                config.SkinColor = color;
            }
        }

        public void UpdateBrowser(float deltaTime)
        {
            if (config == null) LoadConfig();
            EnsureBrowser();
            _browser.Update(deltaTime);
        }

        public void SetNickname(string value)
        {
            nickname = value ?? "";
            if (string.IsNullOrWhiteSpace(nickname)) return;
            config.PlayerNickname = nickname.Trim();
            SaveConfig();
        }

        public void SelectColor(int index)
        {
            if (index < 0 || index >= availableColors.Length) return;
            selectedColorIndex = index;
            ChangePlayerTexture(availableColors[index]);
            SaveConfig();
        }

        public void JoinOrCancel()
        {
            if (ClientConnector.IsConnecting) ClientConnector.Cancel();
            else Join(Selected);
        }

        public void Back()
        {
            menu.SetStateToMain();
        }

        public static Vector4 ColorOf(string colorName) => GetColorVector(colorName);

        public bool IsListed(ServerListItem item)
        {
            foreach (var saved in _browser.Saved)
                if (saved == item) return true;
            foreach (var local in _browser.Local)
                if (local == item) return true;
            return false;
        }

        public void SaveServer(ServerListItem item, string name, string host, int port)
        {
            ServerEntry entry;
            if (item != null && item.IsSaved)
            {
                entry = item.Entry;
                entry.Name = name;
                entry.IP = host;
                entry.Port = port;
            }
            else
            {
                entry = new ServerEntry
                {
                    Name = name,
                    IP = host,
                    Port = port,
                    Description = item?.Entry.Description ?? "",
                    IconHash = item?.Entry.IconHash ?? ""
                };
                config.Servers.Add(entry);
            }

            SaveConfig();
            _browser.SetSavedServers(config.Servers);

            foreach (var saved in _browser.Saved)
            {
                if (saved.Entry != entry) continue;
                Selected = saved;
                _browser.Ping(saved);
                break;
            }
        }

        public void RemoveServer(ServerListItem item)
        {
            if (item == null || !item.IsSaved) return;

            config.Servers.Remove(item.Entry);
            SaveConfig();
            _browser.SetSavedServers(config.Servers);
            Selected = null;
        }

        private void EnsureBrowser()
        {
            if (_browser != null) return;

            _browser = new ServerBrowser(new LiteNetQuerySocket());
            _browser.HasIcon = _icons.Has;
            _browser.IconReceived += _icons.Store;
            _browser.SavedEntriesChanged += SaveConfig;
            _browser.KnownLocalChanged += SaveConfig;
            _browser.DirectedBroadcasts = LocalBroadcastAddresses.Collect();
        }

        public void Join(ServerListItem item)
        {
            if (item == null) return;

            string error = JoinPrecheck.Check(item.Status, Engine.Application.Version);
            if (error != null)
            {
                ConnectionScreen.ShowError("Cannot join", error);
                return;
            }

            Connect(item.Entry, item.Status?.Name ?? item.DisplayName);
        }

        private void Connect(ServerEntry server, string displayName)
        {
            if (ClientConnector.IsConnecting) return;

            GameSession.SetPlayer(config.PlayerNickname, config.SkinColor);
            Debug.Log($"[Network] Joining {displayName} {server.IP}:{server.Port}");

            ConnectionScreen.Show($"Joining {displayName}");
            ClientConnector.Connect(server.IP, server.Port, OnJoinAccepted, reason => ConnectionScreen.ShowError("Connection failed", reason));
        }

        private void OnJoinAccepted(RemoteWorldInfo world)
        {
            Input.HideCursor();
            SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(world.ToSceneArgs(config.SkinColor, config.PlayerNickname));
        }

        private static Vector4 GetColorVector(string colorName)
        {
            return colorName switch
            {
                "Yellow" => new Vector4(1f, 1f, 0f, 1f),
                "Orange" => new Vector4(1f, 0.64f, 0f, 1f),
                "Purple" => new Vector4(0.5f, 0f, 0.5f, 1f),
                "Blue" => new Vector4(0f, 0f, 1f, 1f),
                "Green" => new Vector4(0f, 1f, 0f, 1f),
                "Cyan" => new Vector4(0f, 1f, 1f, 1f),
                "Red" => new Vector4(1f, 0f, 0f, 1f),
                "White" => new Vector4(1f, 1f, 1f, 1f),
                "Black" => new Vector4(0.1f, 0.1f, 0.1f, 1f),
                _ => new Vector4(1f, 1f, 1f, 1f)
            };
        }

        public void Dispose()
        {
            _browser?.Dispose();
            _browser = null;
        }
    }
}
