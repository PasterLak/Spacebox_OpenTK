// Overlay.cs
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Common;
using Spacebox.Common.Audio;
using Spacebox.Game;
using System.Diagnostics;

using NumVector4 = System.Numerics.Vector4;
using OpenTKVector4 = OpenTK.Mathematics.Vector4;

namespace Spacebox.GUI
{
    public class Overlay
    {
        public static bool IsVisible = false;
        public static Block? AimedBlock = null;

        public static NumVector4 Red = new NumVector4(1f, 0f, 0f, 1f);
        public static NumVector4 Yellow = new NumVector4(1f, 1f, 0f, 1f);
        public static NumVector4 Green = new NumVector4(0f, 1f, 0f, 1f);
        public static NumVector4 Orange = new NumVector4(1f, 0.5f, 0f, 1f);

        private const float MemData = 1024.0f * 1024.0f;
        private static long memoryBytes;
        private static double memoryMB;

        public static void OnGUI()
        {
            if (!IsVisible) return;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0.8f));
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove);
            ImGui.SetWindowPos(new System.Numerics.Vector2(20, 40), ImGuiCond.Always);

            ImGui.Spacing();
            ImGui.Text($" ");

            // FPS с цветовой индикацией
            float fps = Time.FPS;
            NumVector4 fpsColor;

            if (fps < 20f)
            {
                fpsColor = Red;
            }
            else if (fps < 40f)
            {
                fpsColor = Orange;
            }
            else if (fps < 60f)
            {
                fpsColor = Yellow;
            }
            else
            {
                fpsColor = Green;
            }

            ImGui.TextColored(fpsColor, $"FPS: {fps}");

            // Отображение использования памяти
            Process currentProcess = Process.GetCurrentProcess();

            // Private Working Set
            memoryBytes = currentProcess.WorkingSet64;
            memoryMB = memoryBytes / MemData;
            ImGui.TextColored(new NumVector4(0.56f, 0.8f, 0.8f, 1f), $"Memory Usage: {memoryMB:F2} MB");

          

            if (Camera.Main != null)
            {
                var cam = Camera.Main;
                Vector3i playerPosInt = new Vector3i((int)cam.Position.X, (int)cam.Position.Y, (int)cam.Position.Z);
                ImGui.Text($"Camera Position: {playerPosInt}");

                Astronaut ast = (cam as Astronaut);

                if (ast != null)
                {
                    string formatted = ast.InertiaController.Velocity.Length.ToString("0.0");
                    ImGui.Text($"Speed: {formatted}");
                }
            }

            if (AimedBlock != null)
            {
                ImGui.Text($" ");
                ImGui.Text($"Block");
                ImGui.Text(AimedBlock.ToString());
            }

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
            ImGui.Text($"Render: {Time.RenderTimePercent} %, Update: {Time.UpdateTimePercent}%, OnGUI: {Time.OnGUITimePercent}%");
            ImGui.Text($" ");
            ImGui.Text($"Console On (F1): {Common.Debug.IsVisible}");
            ImGui.Text($"Debug On (F4): {VisualDebug.ShowDebug}");
            ImGui.Text($"FrameLimiter On (F7): {(FrameLimiter.TargetFPS == 120 ? true : false)}");
            ImGui.Text($"Wireframe Mode (F10) ");
            ImGui.Text($" ");
            ImGui.Text($"Shaders Cached: {ShaderManager.Count}");
            ImGui.Text($"Textures Cached: {TextureManager.Count}");
            ImGui.Text($"Sounds Cached: {SoundManager.Count}");
            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
