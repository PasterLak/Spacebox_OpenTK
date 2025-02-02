using System;
using System.Numerics;
using System.IO;
using System.Text.Json;
using ImGuiNET;
using Engine;
using Engine.Audio;

namespace Spacebox.Game.GUI.Menu
{
    public class AddServerWindow : MenuWindow
    {
        private MultiplayerWindow parent;
        private string serverName = "";
        private string serverIP = "";
        private int serverPort = 7777;
        private string playerName = "";
        private bool isEditMode = false;
        private ServerInfo editingServer = null;
        public AddServerWindow(MultiplayerWindow parent)
        {
            this.parent = parent;
        }
        public void SetEditMode(ServerInfo server)
        {
            if (server == null)
            {
                isEditMode = false;
                editingServer = null;
                serverName = "";
                serverIP = "";
                serverPort = 7777;
                playerName = parent.GetConfig().PlayerNickname;
            }
            else
            {
                isEditMode = true;
                editingServer = server;
                serverName = server.Name;
                serverIP = server.IP;
                serverPort = server.Port;
                playerName = server.PlayerName;
            }
        }
        public override void Render()
        {
            Vector2 windowSize = ImGui.GetIO().DisplaySize;
            float windowWidth = windowSize.X * 0.3f;
            float windowHeight = windowSize.Y * 0.4f;
            Vector2 windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(1, 1, 1, 0f));
            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
            ImGui.Begin("Add Server", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
            GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), windowSize.Y, 0.004f);
            float inputWidth = windowWidth * 0.8f;
            float inputHeight = windowHeight * 0.06f;
            float spacing = windowHeight * 0.03f;
            float labelHeight = ImGui.CalcTextSize("A").Y;
            float totalHeight = (labelHeight + inputHeight + spacing) * 4 + spacing + 40;
            float topPadding = (windowHeight - totalHeight) / 2;
            ImGui.Dummy(new Vector2(0, topPadding));
            parent.Menu.CenterInputText("Server Name", ref serverName, 100, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            parent.Menu.CenterInputText("Server IP", ref serverIP, 50, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            {
                float winW = ImGui.GetWindowWidth();
                Vector2 labelSizePort = ImGui.CalcTextSize("Port");
                ImGui.SetCursorPosX((winW - labelSizePort.X) * 0.5f);
                ImGui.Text("Port");
                ImGui.Dummy(new Vector2(0, spacing * 0.5f));
                ImGui.SetCursorPosX((winW - inputWidth) * 0.5f);
                ImGui.SetNextItemWidth(inputWidth);
                ImGui.InputInt("##Port", ref serverPort);
            }
            ImGui.Dummy(new Vector2(0, spacing));
            parent.Menu.CenterInputText("Player Name", ref playerName, 50, inputWidth, inputHeight);
            ImGui.Dummy(new Vector2(0, spacing));
            float buttonWidth = windowWidth * 0.3f;
            float buttonHeight = 40;
            float totalButtonWidth = buttonWidth * 2 + spacing;
            float buttonY = ImGui.GetCursorPosY() + spacing;
            float winW2 = ImGui.GetWindowWidth();
            float buttonStartX = (winW2 - totalButtonWidth) / 2;
            parent.Menu.ButtonWithBackground(isEditMode ? "Save" : "Add", new Vector2(buttonWidth, buttonHeight),
                new Vector2(buttonStartX, buttonY), () =>
                {
                    var config = parent.GetConfig();
                    if (isEditMode && editingServer != null)
                    {
                        editingServer.Name = serverName;
                        editingServer.IP = serverIP;
                        editingServer.Port = serverPort;
                        editingServer.PlayerName = playerName;
                    }
                    else
                    {
                        var newServer = new ServerInfo
                        {
                            Name = serverName,
                            IP = serverIP,
                            Port = serverPort,
                            PlayerName = playerName
                        };
                        config.Servers.Add(newServer);
                    }
                    parent.SaveConfig();
                    parent.ShowAddServerWindow = false;
                });
            parent.Menu.ButtonWithBackground("Cancel", new Vector2(buttonWidth, buttonHeight),
                new Vector2(buttonStartX + buttonWidth + spacing, buttonY), () =>
                {
                    parent.ShowAddServerWindow = false;
                });
            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
