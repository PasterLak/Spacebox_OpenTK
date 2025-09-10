using System.Numerics;
using ImGuiNET;
using Engine;

namespace Spacebox.Game.GUI.Menu
{
    public class DevLogWindow
    {

        public static DevLogWindow Instance { get;  set; }
        private bool _isVisible = false;
        private bool isMinimized = false;
        private string logText = "Development Log\n\n";
        private Vector2 scrollPosition = Vector2.Zero;
        private float lastWindowHeight = 0;

        public DevLogWindow()
        {
            Instance = this;
        }

        public void Render()
        {
            if (!_isVisible) return;

            var displaySize = ImGui.GetIO().DisplaySize;
            var windowWidth = displaySize.X * 0.2f;
            var windowHeight = displaySize.Y * 0.6f;
            var windowPosX = 0f;
            var windowPosY = displaySize.Y * 0.3f;
            var minimizedHeight = 30f;

            if (isMinimized)
            {
                ImGui.SetNextWindowPos(new Vector2(windowPosX, windowPosY));
                ImGui.SetNextWindowSize(new Vector2(windowWidth, minimizedHeight));

                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.7f));
                ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0, 0, 0, 0.8f));
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0, 0, 0, 0.9f));

                if (ImGui.Begin("Dev Log", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
                {
                    if (ImGui.Button("Expand", new Vector2(windowWidth - 20, 20)))
                    {
                        isMinimized = false;
                    }
                }
                ImGui.End();

                ImGui.PopStyleColor(3);
            }
            else
            {
                ImGui.SetNextWindowPos(new Vector2(windowPosX, windowPosY));
                ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));

                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.7f));
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0.7f));
                ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0, 0, 0, 0.8f));
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0, 0, 0, 0.9f));
                ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0.1f, 0.1f, 0.1f, 0.5f));
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.3f, 0.3f, 0.3f, 0.8f));
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

                if (ImGui.Begin("Dev Log", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
                {
                    var contentRegion = ImGui.GetContentRegionAvail();
                    var buttonHeight = 25f;
                    var spacing = 5f;

                    if (ImGui.Button("Close", new Vector2(60, buttonHeight)))
                    {
                        isMinimized = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Hide", new Vector2(60, buttonHeight)))
                    {
                        _isVisible = false;
                    }

                    ImGui.Separator();

                    var textHeight = contentRegion.Y - buttonHeight - spacing * 2 - 20;

                    if (ImGui.BeginChild("LogText", new Vector2(contentRegion.X, textHeight), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
                    {
                        ImGui.SetWindowFontScale(0.7f);
                        ImGui.TextUnformatted(logText);

                        if (Math.Abs(ImGui.GetWindowHeight() - lastWindowHeight) > 1.0f)
                        {
                            ImGui.SetScrollHereY(1.0f);
                            lastWindowHeight = ImGui.GetWindowHeight();
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.End();

                ImGui.PopStyleColor(8);
            }
        }

        public void AddLogEntry(string message)
        {
            logText += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        public void AddLogFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string fileContent = File.ReadAllText(filePath);

                    logText += fileContent + "\n";
                }
                else
                {
                    AddLogEntry($"File not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                AddLogEntry($"Error reading file {filePath}: {ex.Message}");
            }
        }

        public void ClearLog()
        {
            logText = "Development Log\n\n";
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public bool IsMinimized => isMinimized;
    }
}