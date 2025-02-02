using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Engine;
using Engine.Audio;

namespace Spacebox.Game.GUI.Menu
{
    public class MultiplayerWindow : MenuWindow
    {
        private GameMenu menu;
        private ClientConfig config;
        private const string ConfigFileName = "client.json";
        private int selectedServerIndex = -1;
        public bool ShowAddServerWindow = false;
        public AddServerWindow addServerWindow;
        private string nickname;
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
        public ClientConfig GetConfig() => config;
        public GameMenu Menu => menu;
        public override void Render()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            Vector2 windowPos = GameMenu.CenterNextWindow2(windowWidth, windowHeight);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1, 1, 1, 0f));
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Multiplayer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            float inputWidth = windowWidth * 0.8f;
            float inputHeight = windowHeight * 0.08f;
            float spacing = windowHeight * 0.03f;
            Menu.CenterInputText("Player Nickname", ref nickname, 50, inputWidth, inputHeight);
            config.PlayerNickname = nickname;
            SaveConfig();
            ImGui.Separator();
            ImGui.Text("Saved Servers:");
            ImGui.BeginChild("ServerList", new Vector2(windowWidth - 20, windowHeight * 0.4f));
            for (int i = 0; i < config.Servers.Count; i++)
            {
                var server = config.Servers[i];
                bool isSelected = (selectedServerIndex == i);
                if (ImGui.Selectable(server.Name, isSelected))
                {
                    selectedServerIndex = i;
                }
            }
            ImGui.EndChild();
            if (selectedServerIndex >= 0 && selectedServerIndex < config.Servers.Count)
            {
                ImGui.Separator();
                if (ImGui.Button("Join", new Vector2(100, 30)))
                {
                    Debug.Log("Joining server: " + config.Servers[selectedServerIndex].Name);
                }
                ImGui.SameLine();
                if (ImGui.Button("Edit", new Vector2(100, 30)))
                {
                    ShowAddServerWindow = true;
                    addServerWindow.SetEditMode(config.Servers[selectedServerIndex]);
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete", new Vector2(100, 30)))
                {
                    config.Servers.RemoveAt(selectedServerIndex);
                    selectedServerIndex = -1;
                    SaveConfig();
                }
            }
            ImGui.Separator();
            float btnWidth = 120, btnHeight = 30;
            float winW = ImGui.GetWindowWidth();
            float xPos = (winW - btnWidth * 2 - spacing) / 2;
            Menu.ButtonWithBackground("Back", new Vector2(btnWidth, btnHeight), new Vector2(xPos, ImGui.GetCursorPosY()), () =>
            {
                menu.SetStateToMain();
            });
            ImGui.SameLine();
            Menu.ButtonWithBackground("Add Server", new Vector2(btnWidth, btnHeight), new Vector2(xPos + btnWidth + spacing, ImGui.GetCursorPosY()), () =>
            {
                ShowAddServerWindow = true;
                addServerWindow.SetEditMode(null);
            });
            ImGui.End();
            ImGui.PopStyleColor();
            if (ShowAddServerWindow)
            {
                addServerWindow.Render();
            }
        }
    }
}
