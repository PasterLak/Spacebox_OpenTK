using ImGuiNET;
using OpenTK.Mathematics;
using Engine;
using Engine.InputPro;
namespace Spacebox.Game.GUI
{
    public class InputOverlay
    {

        public static bool IsVisible = false;

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


            ImGui.Text($"Press F6 to hide/show");
            ImGui.Separator();
            ImGui.Text($"[Debug]");
            ImGui.Text($"");
            ImGui.Text($"[^] Console");
            ImGui.Text($"[F3] Show FPS");
            ImGui.Text($"[F4] Visual Debug");
            ImGui.Text($"[F7] FPS Limiter Off");
            ImGui.Text($"[F8] Hide Interface");
            ImGui.Text($"[F9] UI Debugger");
            ImGui.Text($"[F10] Wireframe Mesh On");
            ImGui.Text($"[F11] Fullscreen");
            ImGui.Text($"[F12] Screenshot");
            ImGui.Text($"");
            ImGui.Text($"[Game]");
            ImGui.Text($"");

            if(InputManager.Instance != null)
            foreach ((_ ,var action) in InputManager.Instance.GetAllActions())
            {
                if(action.HasBindings) 
                ImGui.Text($"[{action.Bindings[0].GetDisplayName()}] {action.Name}");
            }

            ImGui.Text($"");
            ImGui.Text($"[Alt+F4] Close the game");

            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
