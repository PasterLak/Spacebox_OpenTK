﻿using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using Spacebox.Common;
using Spacebox.Entities;

namespace Spacebox.GUI
{
    public class InputOverlay
    {
        public static bool IsVisible = false;
        private static bool WireframeModeEnabled = false;

        public static void OnGUI()
        {
           

            if (!IsVisible) return;

            var io = ImGui.GetIO();

            var x = Window.Instance.Size.X;
            var y = Window.Instance.Size.Y;

            Vector2 windowSize = new Vector2(x * 0.2f, y * 0.98f);
            Vector2 windowPos = new Vector2(io.DisplaySize.X - windowSize.X - 20, y * 0.01f);

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(windowPos.X, windowPos.Y), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(windowSize.X, windowSize.Y), ImGuiCond.Always);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0.8f));
            ImGui.Begin("InputOverlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);


         
            //ImGui.Separator();

            ImGui.Text($"Press F6 to hide/show");
            ImGui.Separator();
            ImGui.Text($"[Debug]");
            ImGui.Text($"");
            ImGui.Text($"[^] Console");
            ImGui.Text($"[F3] Show FPS");
            ImGui.Text($"[F4] Visual Debug");
            ImGui.Text($"[F7] FPS Limiter on/off");
            ImGui.Text($"[F8] Show/Hide Interface");
            ImGui.Text($"[F9] UI Debugger on/off");
            ImGui.Text($"[F10] Wireframe Mesh on/off");
            ImGui.Text($"[F11] Fullscreen");
            ImGui.Text($"[F12] Screenshot");
            ImGui.Text($"[I] Inertia on/off");
            ImGui.Text($"");
            ImGui.Text($"[Player]");
            ImGui.Text($"");
            ImGui.Text($"[W,S,A,D] Move");
            ImGui.Text($"[Space] Up");
            ImGui.Text($"[Ctrl] Down");
            ImGui.Text($"[Shift] Speed-up");
            ImGui.Text($"[Q,E] Camera roll");
            ImGui.Text($"[C] Inventory");
            ImGui.Text($"[G] Drop item");
            ImGui.Text($"[F] Flashlight");
      

            ImGui.Text($"");
            ImGui.Text($"[Inventory]");
            ImGui.Text($"");
            ImGui.Text($"[X+Click] Delete slot item");
            ImGui.Text($"[Shift+Click] Move item to another storage");
            ImGui.Text($"[Alt+Click] Split the stack");
            ImGui.Text($"");

            ImGui.Text($"[Building]");
            ImGui.Text($"");
            ImGui.Text($"[P] Save the world");
            ImGui.Text($"[Numpad9] Delete all chunk blocks");
            ImGui.Text($"");
            ImGui.Text($"[Scroll] Select block");
            ImGui.Text($"[LMB] Destroy block");
            ImGui.Text($"[RMB] Place block");

            ImGui.Text($"");
            ImGui.Text($"[ESC] Close the game");

            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
