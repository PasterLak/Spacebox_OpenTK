using Engine;
using Engine.Networking.Query;
using Engine.SceneManagement;
using ImGuiNET;
using Spacebox.Network;
using Spacebox.Network.Discovery;
using Spacebox.Scenes;
using System.Numerics;

namespace Spacebox.Game.GUI.Menu
{
    public class MultiplayerWindow : MenuWindow, IDisposable
    {
        private static readonly Vector4 TitleColor = new Vector4(1f, 0.8f, 0.2f, 1f);

        public GameMenu menu;
        private ClientConfig config;
        private string nickname = "";

        private readonly string[] availableColors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
        private int selectedColorIndex = 0;

        private readonly ServerIconCache _icons = new ServerIconCache();
        private readonly ServerListView _list;
        private readonly ServerInfoWindow _info;
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

        public ServerListItem Selected
        {
            get => _list.Selected;
            set => _list.Selected = value;
        }

        public bool CanEdit(ServerListItem item) => item != null && IsListed(item);

        protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.MultiplayerPage(this);

        public MultiplayerWindow(GameMenu menu)
        {
            this.menu = menu;
            _list = new ServerListView(_icons);
            _list.Activated += Join;
            _info = new ServerInfoWindow(menu, _icons, SaveServer, Join, item => _browser?.Ping(item));
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
            else Join(_list.Selected);
        }

        public void Back()
        {
            _info.Close();
            menu.SetStateToMain();
        }

        public static Vector4 ColorOf(string colorName) => GetColorVector(colorName);

        public override void Render()
        {
            UpdateBrowser(Time.Delta);

            var display = ImGui.GetIO().DisplaySize;
            float width = MathF.Min(MathF.Max(display.X * 0.5f, 620f), display.X * 0.95f);
            float height = MathF.Min(MathF.Max(display.Y * 0.68f, 460f), display.Y * 0.95f);
            var position = GameMenu.CenterNextWindow2(width, height);

            ImGui.Begin("Multiplayer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

            ImGui.BeginDisabled(_info.IsOpen);

            float padding = MathF.Max(12f, height * 0.03f);
            float buttonHeight = MathF.Max(28f, display.Y * 0.045f);
            float contentWidth = width - padding * 2f;

            ImGui.SetCursorPos(new Vector2(padding, padding));
            ImGui.TextColored(TitleColor, "Multiplayer");
            DrawProfile(padding, contentWidth);

            float listTop = ImGui.GetCursorPosY() + padding * 0.5f;
            float buttonsTop = height - padding - buttonHeight * 2f - padding * 0.5f;
            ImGui.SetCursorPos(new Vector2(padding, listTop));
            _list.Draw(_browser, new Vector2(contentWidth, buttonsTop - listTop - padding * 0.5f));

            DrawButtons(padding, contentWidth, buttonsTop, buttonHeight);

            ImGui.EndDisabled();
            ImGui.End();

            _info.Render();
        }

        private void DrawProfile(float padding, float contentWidth)
        {
            float y = ImGui.GetCursorPosY() + padding * 0.4f;
            float frame = ImGui.GetFrameHeight();
            float textOffset = (frame - ImGui.GetTextLineHeight()) * 0.5f;
            float nicknameLabel = ImGui.CalcTextSize("Nickname  ").X;
            float colorLabel = ImGui.CalcTextSize("  Color  ").X;
            float colorWidth = contentWidth * 0.28f;
            float nicknameWidth = contentWidth - nicknameLabel - colorLabel - colorWidth;

            ImGui.SetCursorPos(new Vector2(padding, y + textOffset));
            ImGui.Text("Nickname");
            ImGui.SetCursorPos(new Vector2(padding + nicknameLabel, y));
            ImGui.SetNextItemWidth(nicknameWidth);
            bool nicknameChanged = ImGui.InputText("##nickname", ref nickname, 32);
            nicknameChanged |= ClipboardMenu.ForTextField("##nicknameMenu", ref nickname, 32);
            if (nicknameChanged && !string.IsNullOrWhiteSpace(nickname))
            {
                config.PlayerNickname = nickname.Trim();
                SaveConfig();
            }

            ImGui.SetCursorPos(new Vector2(padding + nicknameLabel + nicknameWidth, y + textOffset));
            ImGui.Text("  Color");
            ImGui.SetCursorPos(new Vector2(padding + nicknameLabel + nicknameWidth + colorLabel, y));
            ImGui.SetNextItemWidth(colorWidth);
            DrawColorCombo(frame);

            ImGui.SetCursorPos(new Vector2(padding, y + frame));
        }

        private void DrawColorCombo(float frameHeight)
        {
            string currentItem = availableColors[selectedColorIndex];

            if (ImGui.BeginCombo("##skinColor", ""))
            {
                for (int i = 0; i < availableColors.Length; i++)
                {
                    bool isSelected = selectedColorIndex == i;

                    ImGui.ColorButton($"##clr_btn_{i}", GetColorVector(availableColors[i]), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(frameHeight, frameHeight));
                    ImGui.SameLine();

                    if (ImGui.Selectable(availableColors[i], isSelected))
                    {
                        selectedColorIndex = i;
                        ChangePlayerTexture(availableColors[i]);
                        SaveConfig();
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            var drawList = ImGui.GetWindowDrawList();
            var min = ImGui.GetItemRectMin();
            var style = ImGui.GetStyle();

            float iconSize = frameHeight - style.FramePadding.Y * 2;
            var iconPos = new Vector2(min.X + style.FramePadding.X, min.Y + style.FramePadding.Y);

            drawList.AddRectFilled(iconPos, iconPos + new Vector2(iconSize, iconSize), ImGui.GetColorU32(GetColorVector(currentItem)), style.FrameRounding);
            drawList.AddText(new Vector2(iconPos.X + iconSize + style.ItemInnerSpacing.X, iconPos.Y), ImGui.GetColorU32(ImGuiCol.Text), currentItem);
        }

        private void DrawButtons(float padding, float contentWidth, float y, float height)
        {
            float spacing = padding * 0.6f;
            float width = (contentWidth - spacing * 3f) / 4f;
            var selected = _list.Selected;
            bool hasSelection = selected != null && IsListed(selected);
            bool connecting = ClientConnector.IsConnecting;

            ImGui.BeginDisabled(!hasSelection && !connecting);
            menu.ButtonWithBackground(connecting ? "Cancel" : "Join", new Vector2(width, height), new Vector2(padding, y), () =>
            {
                if (ClientConnector.IsConnecting) ClientConnector.Cancel();
                else Join(_list.Selected);
            });
            ImGui.EndDisabled();

            ImGui.BeginDisabled(!hasSelection);
            menu.ButtonWithBackground("Info", new Vector2(width, height), new Vector2(padding + (width + spacing), y), () => _info.OpenInfo(_list.Selected));
            ImGui.EndDisabled();

            menu.ButtonWithBackground("Add server", new Vector2(width, height), new Vector2(padding + (width + spacing) * 2f, y), _info.OpenAdd);

            ImGui.BeginDisabled(!hasSelection || !selected.IsSaved);
            menu.ButtonWithBackground("Remove", new Vector2(width, height), new Vector2(padding + (width + spacing) * 3f, y), () => RemoveServer(_list.Selected));
            ImGui.EndDisabled();

            float bottomY = y + height + padding * 0.5f;
            float halfWidth = (contentWidth - spacing) * 0.5f;
            menu.ButtonWithBackground("Refresh", new Vector2(halfWidth, height), new Vector2(padding, bottomY), () => _browser.Refresh());
            menu.ButtonWithBackground("Back", new Vector2(halfWidth, height), new Vector2(padding + halfWidth + spacing, bottomY), () =>
            {
                _info.Close();
                menu.SetStateToMain();
            });
        }

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
                _list.Selected = saved;
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
            _list.Selected = null;
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
