// Overlay.cs
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Entities;

namespace Spacebox.GUI
{
    public class Overlay
    {

        public static bool IsVisible = false;


        public static void OnGUI(Node3D player)
        {
            if(Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F2))
            {
                IsVisible = !IsVisible;
            }
            if (!IsVisible) return;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0.8f));
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove);
            ImGui.SetWindowPos(new System.Numerics.Vector2(20, 40), ImGuiCond.Always);

            ImGui.Spacing();
            ImGui.Text($" ");
            ImGui.Text($"FPS: {Time.FPS}");

            Vector3i playerPosInt = new Vector3i((int)player.Position.X, (int)player.Position.Y, (int)player.Position.Z);
            ImGui.Text($"Camera Position: {playerPosInt}");
            ImGui.Text($" ");
            ImGui.Text($"[GAME LOOP DEBUG]");
            ImGui.Text($" ");
            ImGui.Text($"Render Time: {Time.RenderTime:F2} ms");
            ImGui.Text($"Avg Render Time: {Time.AverageRenderTime:F2} ms");
            ImGui.Text($"Update Time: {Time.UpdateTime:F2} ms");
            ImGui.Text($"Avg Update Time: {Time.AverageUpdateTime:F2} ms");
            ImGui.Text($"OnGUI Time: {Time.OnGUITime:F2} ms");
            ImGui.Text($"Avg OnGUI Time: {Time.AverageOnGUITime:F2} ms");
            ImGui.Text($" ");
            ImGui.Text($"Console On (F1): {GameConsole.IsVisible}");
            ImGui.Text($"Debug On (F4): {Debug.ShowDebug}");
            ImGui.Text($"FrameLimiter On (F7): {(FrameLimiter.TargetFPS == 120 ? true : false)}");
            ImGui.Text($"Wireframe Mode (F10) ");
            ImGui.Text($" ");
            ImGui.Text($"Shaders Cached: {ShaderManager.Count}");
            ImGui.Text($"Textures Cached: {TextureManager.Count}");
            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
