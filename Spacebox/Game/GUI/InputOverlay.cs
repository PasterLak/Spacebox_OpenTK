using ImGuiNET;
using OpenTK.Mathematics;
using Engine;
namespace Spacebox.Game.GUI
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
            ImGui.Text($"[F7] FPS Limiter Off");
            ImGui.Text($"[F8] Hide Interface");
            ImGui.Text($"[F9] UI Debugger");
            ImGui.Text($"[F10] Wireframe Mesh On");
            ImGui.Text($"[F11] Fullscreen");
            ImGui.Text($"[F12] Screenshot");
            ImGui.Text($"");
            ImGui.Text($"[Player]");
            ImGui.Text($"");
            ImGui.Text($"[W,S,A,D] Move");
            ImGui.Text($"[Space] Up");
            ImGui.Text($"[Ctrl] Down");
            ImGui.Text($"[Shift] Speed-up");
            ImGui.Text($"[Q,E] Camera roll");
            ImGui.Text($"[Tab] Inventory");
            ImGui.Text($"[G] Drop item");
            ImGui.Text($"[F] Flashlight");


            ImGui.Text($"");
            ImGui.Text($"[Inventory]");
            ImGui.Text($"");
            ImGui.Text($"[X+Click] Delete slot item");
            ImGui.Text($"[Shift+Click] Move item to another storage");
            ImGui.Text($"");

            ImGui.Text($"[Building]");
            ImGui.Text($"");
            ImGui.Text($"[P] Save the world");

            ImGui.Text($"");
            ImGui.Text($"[Alt+F4] Close the game");

            ImGui.End();
            ImGui.PopStyleColor();
        }
    }
}
