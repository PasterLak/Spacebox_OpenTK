using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using ImGuiNET;
using Engine;
using Engine.Audio;
using Spacebox.Client;

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

        public MultiplayerWindow(GameMenu menu)
        {
            this.menu = menu;
            LoadConfig();
            nickname = config.PlayerNickname;
            addServerWindow = new AddServerWindow(this);
        }

        private void LoadConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                config = JsonSerializer.Deserialize<ClientConfig>(json);
            }
            else
            {
                config = new ClientConfig();
                SaveConfig();
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

        public override void Render()
        {
            StartLocalDiscovery();
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1, 1, 1, 0f));
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Multiplayer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            ImGui.Columns(2, "nicknameColumns", false);
            ImGui.SetColumnWidth(0, windowWidth * 0.4f);
            ImGui.Text("Player Nickname");
            ImGui.NextColumn();
            ImGui.InputText("##nickname", ref nickname, 50);
            ImGui.Columns(1);
            config.PlayerNickname = nickname;
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
            });
            ImGui.SameLine();
            ButtonWithBackground("Join", new Vector2(btnWidth, buttonH), new Vector2(10 + btnWidth + midSpacing, buttonY), () =>
            {
                if (selectedServerIndex >= 0 && selectedServerIndex < combinedServers.Count)
                {
                    string worldName = combinedServers[selectedServerIndex].Name;
                    var existingWorld = menu.Worlds.FirstOrDefault(w => w.Name == worldName);
                    if (existingWorld == null)
                    {
                        menu.newWorldName = worldName;
                        menu.newWorldAuthor = "";
                        menu.newWorldSeed = "420";
                        menu.SelectedGameSetIndex = 0;
                        menu.SelectedGameModeIndex = 1;
                        menu.CreateNewWorld();
                        menu.LoadWorld(menu.selectedWorld);
                    }
                    else
                    {
                        menu.LoadWorld(existingWorld);
                    }
                }
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

        private void ButtonWithBackground(string label, Vector2 size, Vector2 cursorPos, Action onClick)
        {
            menu.ButtonWithBackground(label, size, cursorPos, onClick);
        }
    }
}
