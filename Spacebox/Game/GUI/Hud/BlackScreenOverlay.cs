using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using ImGuiNET;
using Engine;
namespace Spacebox.GUI
{

    public static class BlackScreenOverlay
    {
        private static bool _isEnabled = false;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static void Initialize()
        {

        }

        public static void OnGUI()
        {
            if (_isEnabled)
            {

                GL.ClearColor(Color4.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


                ImGui.Begin("Black Screen Overlay", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove);
                ImGui.End();
            }
        }

        public static void Shutdown()
        {

        }
    }

}

