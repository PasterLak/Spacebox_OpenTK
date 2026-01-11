using Engine;
using Engine.SceneManagement;
using ImGuiNET;
using Spacebox.Client;
using Spacebox.Game.Generation;
using Spacebox.Scenes;
using SpaceNetwork;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using static Spacebox.Game.Resource.GameSetLoader;

namespace Spacebox.Game.GUI.Menu
{
    public class MultiplayerWindow : MenuWindow
    {
        public GameMenu menu;
        private ClientConfig config;
        private const string ConfigFileName = "client.json";
        private int selectedServerIndex = -1;
        public bool ShowAddServerWindow = false;
        public AddServerWindow addServerWindow;
        private string nickname;
        private Thread discoveryThread;
        private readonly object localServerLock = new object();
        private List<ServerInfo> localServers = new List<ServerInfo>();

        private string[] availableColors = new[] { "Yellow", "Orange", "Purple", "Blue", "Green", "Cyan", "Red", "White", "Black" };
        private int selectedColorIndex = 0;

        public ClientConfig Config => config;
        public MultiplayerWindow(GameMenu menu)
        {
            this.menu = menu;
       
            addServerWindow = new AddServerWindow(this);
         
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
        }

        public ClientConfig LoadConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    config = JsonSerializer.Deserialize<ClientConfig>(json);
                }
                catch
                {
                    config = new ClientConfig();
                }

                if (config.GenerateNameIfEmpty())
                {
                    SaveConfig();
                }

                if (string.IsNullOrEmpty(config.SkinColor))
                {
                    config.SkinColor = "Yellow";
                }
            }
            else
            {
                config = new ClientConfig();
                SaveConfig();
            }

            return config;
        }

        public void ChangePlayerTexture(string color)
        {
            var sceneMenu = Scene.Root as MenuScene;
            if (sceneMenu != null)
            {
                sceneMenu.ChangeAstronautColor(color);
                config.SkinColor = color;
            }
        }

        public void SaveConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void StartLocalDiscovery()
        {
            if (discoveryThread != null && discoveryThread.IsAlive)
                return;
            discoveryThread = new Thread(() =>
            {
                var discovered = LocalServerFinder.DiscoverServers("spacebox" + Application.Version, 5544, 5000);
                lock (localServerLock)
                {
                    localServers.Clear();
                    if (discovered != null && discovered.Count > 0)
                    {
                        foreach (var server in discovered)
                        {
                            server.Name = $"[Local] {server.Name} {server.IP} {server.Port}";
                            localServers.Add(server);
                        }
                    }
                }
            });
            discoveryThread.IsBackground = true;
            discoveryThread.Start();
        }

        public ClientConfig GetConfig() => config;

        private Vector4 GetColorVector(string colorName)
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

        public override void Render()
        {
            StartLocalDiscovery();
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            float windowWidth = windowSize.X * 0.35f;
            float windowHeight = windowSize.Y * 0.45f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1, 1, 1, 0f));
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Multiplayer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);

            ImGui.Columns(4, "playerConfigColumns", false);
            ImGui.SetColumnWidth(0, windowWidth * 0.25f);
            ImGui.SetColumnWidth(1, windowWidth * 0.35f);
            ImGui.SetColumnWidth(2, windowWidth * 0.15f);
            ImGui.SetColumnWidth(3, windowWidth * 0.25f);

            ImGui.Text("Nickname:");
            ImGui.NextColumn();
            ImGui.InputText("##nickname", ref nickname, 32);
            ImGui.NextColumn();

            ImGui.Text("Color:");
            ImGui.NextColumn();

            float frameHeight = ImGui.GetFrameHeight();
            string currentItem = availableColors[selectedColorIndex];

            if (ImGui.BeginCombo("##skinColor", ""))
            {
                for (int i = 0; i < availableColors.Length; i++)
                {
                    bool isSelected = (selectedColorIndex == i);

                    ImGui.ColorButton($"##clr_btn_{i}", GetColorVector(availableColors[i]), ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(frameHeight, frameHeight));
                    ImGui.SameLine();

                    if (ImGui.Selectable(availableColors[i], isSelected))
                    {
                        selectedColorIndex = i;
                        ChangePlayerTexture(availableColors[i]);
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            var drawList = ImGui.GetWindowDrawList();
            var min = ImGui.GetItemRectMin();
            var style = ImGui.GetStyle();

            float iconSz = frameHeight - (style.FramePadding.Y * 2);
            Vector2 iconPos = new Vector2(min.X + style.FramePadding.X, min.Y + style.FramePadding.Y);

            drawList.AddRectFilled(iconPos, iconPos + new Vector2(iconSz, iconSz), ImGui.GetColorU32(GetColorVector(currentItem)), style.FrameRounding);
            drawList.AddText(new Vector2(iconPos.X + iconSz + style.ItemInnerSpacing.X, iconPos.Y), ImGui.GetColorU32(ImGuiCol.Text), currentItem);

            ImGui.Columns(1);

            config.PlayerNickname = nickname;
            config.SkinColor = availableColors[selectedColorIndex];

            SaveConfig();

            ImGui.Separator();
            ImGui.Text("Saved Servers:");
            List<ServerInfo> combinedServers = new List<ServerInfo>();
            combinedServers.AddRange(config.Servers);
            lock (localServerLock)
            {
                foreach (var ls in localServers)
                {
                    if (!config.Servers.Any(s => s.IP == ls.IP && s.Port == ls.Port))
                        combinedServers.Add(ls);
                }
            }
            ImGui.BeginChild("ServerList", new Vector2(windowWidth - 20, windowHeight * 0.4f));
            for (int i = 0; i < combinedServers.Count; i++)
            {
                var server = combinedServers[i];
                bool isSelected = (selectedServerIndex == i);
                if (ImGui.Selectable(server.Name, isSelected))
                    selectedServerIndex = i;
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    Join(combinedServers);
                }
            }
            ImGui.EndChild();
            ImGui.Separator();
            float midSpacing = windowWidth * 0.02f;
            float buttonH = windowHeight * 0.08f;
            float availableW = windowWidth - 20;
            float btnWidth = (availableW - midSpacing * 3) / 4;
            float buttonY = windowHeight * 0.75f;
            ButtonWithBackground("Refresh", new Vector2(btnWidth, buttonH), new Vector2(10, buttonY), () =>
            {
                StartLocalDiscovery();
                Console.WriteLine("Refreshing local servers...");
            });
            ImGui.SameLine();
            ButtonWithBackground("Join", new Vector2(btnWidth, buttonH), new Vector2(10 + btnWidth + midSpacing, buttonY), () =>
            {
                Join( combinedServers);
            });
            ImGui.SameLine();
            ButtonWithBackground("Edit", new Vector2(btnWidth, buttonH), new Vector2(10 + 2 * (btnWidth + midSpacing), buttonY), () =>
            {
                if (selectedServerIndex >= 0 && selectedServerIndex < combinedServers.Count)
                {
                    var server = combinedServers[selectedServerIndex];
                    if (config.Servers.Any(s => s.IP == server.IP && s.Port == server.Port))
                    {
                        ShowAddServerWindow = true;
                        addServerWindow.SetEditMode(server);
                    }
                }
            });
            ImGui.SameLine();
            ButtonWithBackground("Delete", new Vector2(btnWidth, buttonH), new Vector2(10 + 3 * (btnWidth + midSpacing), buttonY), () =>
            {
                if (selectedServerIndex >= 0 && selectedServerIndex < combinedServers.Count)
                {
                    var server = combinedServers[selectedServerIndex];
                    if (config.Servers.Any(s => s.IP == server.IP && s.Port == server.Port))
                    {
                        config.Servers.RemoveAll(s => s.IP == server.IP && s.Port == server.Port);
                        selectedServerIndex = -1;
                        SaveConfig();
                    }
                }
            });
            float bottomMargin = windowHeight * 0.05f;
            float bottomY = windowHeight - (windowHeight * 0.08f) - bottomMargin;
            float availableWidth = windowWidth - 20;
            float btnSpacing = windowWidth * 0.02f;
            float backWidth = (availableWidth - btnSpacing) / 2;
            float addWidth = backWidth;
            ButtonWithBackground("Back", new Vector2(backWidth, windowHeight * 0.08f), new Vector2(10, bottomY), () =>
            {
                menu.SetStateToMain();
            });
            ImGui.SameLine();
            ButtonWithBackground("Add Server", new Vector2(addWidth, windowHeight * 0.08f), new Vector2(10 + backWidth + btnSpacing, bottomY), () =>
            {
                ShowAddServerWindow = true;
                addServerWindow.SetEditMode(null);
            });
            ImGui.End();
            ImGui.PopStyleColor();
            if (ShowAddServerWindow)
                addServerWindow.Render();
        }

        private void Join(List<ServerInfo> combinedServers)
        {
            if (selectedServerIndex >= 0 && selectedServerIndex < combinedServers.Count)
            {
                var server = combinedServers[selectedServerIndex];
                WorldInfo world;
                ModConfig modConfig;
                var existingWorld = menu.Worlds.FirstOrDefault(w => w.Name == server.Name);
                if (existingWorld == null)
                {
                    menu.newWorldName = server.Name;
                    menu.newWorldAuthor = "";
                    menu.newWorldSeed = "420";
                    menu.SelectedGameSetIndex = 0;
                    menu.SelectedGameModeIndex = 1;
                    menu.CreateNewWorld();
                    world = menu.selectedWorld;
                    modConfig = new ModConfig { ModId = world.ModId, FolderName = world.FolderName };
                }
                else
                {
                    world = existingWorld;
                    modConfig = new ModConfig { ModId = world.ModId, FolderName = "Default" };
                }
                ServerInfo serverInfo = new ServerInfo { Name = server.Name, IP = server.IP, Port = server.Port };
                string appKey = Application.Version;

                Debug.Success("Joining server: " + serverInfo.Name + " " + serverInfo.IP + ":" + serverInfo.Port);
                SceneLauncher.LaunchMultiplayerGame(world, modConfig, serverInfo, config, appKey);
            }
        }

        private void ButtonWithBackground(string label, Vector2 size, Vector2 cursorPos, Action onClick)
        {
            menu.ButtonWithBackground(label, size, cursorPos, onClick);
        }
    }
}