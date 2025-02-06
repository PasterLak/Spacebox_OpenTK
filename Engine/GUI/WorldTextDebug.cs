using ImGuiNET;
using System.Numerics;

namespace Engine.GUI
{
    public static class WorldTextDebug
    {

        //private static string _text = "Press F to use";

        public static bool IsVisible { get; private set; } = true;

        private static Vector4 _color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);

        private static Vector4 red = new Vector4(1.0f, 0, 0, 1.0f);
        private static Vector4 green = new Vector4(0, 1, 0, 1.0f);
        private static Vector4 blue = new Vector4(0, 0, 1, 1.0f);

        public static void SetColor(Vector4 color)
        {
            _color = color;
        }


        public static void Show()
        {
            if (IsVisible) return;

            IsVisible = true;
        }

        public static void Hide()
        {
            if (!IsVisible) return;

            IsVisible = false;
        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
        }


        public static void OnGUI()
        {

            if (!IsVisible)
                return;

            if (Camera.Main == null) return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("CenteredTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);


            //Vector2 textSize = ImGui.CalcTextSize(_text);



            Vector2 posCenter = Camera.Main.WorldToScreenPoint(Vector3.Zero.ToOpenTKVector3(),
                (int)ImGui.GetIO().DisplaySize.X,
               (int)ImGui.GetIO().DisplaySize.Y).ToSystemVector2();

            Vector2 posX = Camera.Main.WorldToScreenPoint(new Vector3(10, 0, 0).ToOpenTKVector3(),
               (int)ImGui.GetIO().DisplaySize.X,
              (int)ImGui.GetIO().DisplaySize.Y).ToSystemVector2();

            Vector2 posY = Camera.Main.WorldToScreenPoint(new Vector3(0, 10, 0).ToOpenTKVector3(),
               (int)ImGui.GetIO().DisplaySize.X,
              (int)ImGui.GetIO().DisplaySize.Y).ToSystemVector2();

            Vector2 posZ = Camera.Main.WorldToScreenPoint(new Vector3(0, 0, 10).ToOpenTKVector3(),
               (int)ImGui.GetIO().DisplaySize.X,
              (int)ImGui.GetIO().DisplaySize.Y).ToSystemVector2();


            //float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            //float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.5f;

            if (posCenter != Vector2.Zero)
            {
                ImGui.SetCursorPos(new Vector2(posCenter.X, posCenter.Y));

                ImGui.PushStyleColor(ImGuiCol.Text, _color);
                ImGui.TextUnformatted("(0,0,0)");
                ImGui.PopStyleColor();
            }

            if (posX != Vector2.Zero)
            {
                ImGui.SetCursorPos(new Vector2(posX.X, posX.Y));

                ImGui.PushStyleColor(ImGuiCol.Text, red);
                ImGui.TextUnformatted("X");
                ImGui.PopStyleColor();
            }
            if (posY != Vector2.Zero)
            {
                ImGui.SetCursorPos(new Vector2(posY.X, posY.Y));

                ImGui.PushStyleColor(ImGuiCol.Text, green);
                ImGui.TextUnformatted("Y");
                ImGui.PopStyleColor();
            }
            if (posZ != Vector2.Zero)
            {
                ImGui.SetCursorPos(new Vector2(posZ.X, posZ.Y));

                ImGui.PushStyleColor(ImGuiCol.Text, blue);
                ImGui.TextUnformatted("Z");
                ImGui.PopStyleColor();
            }
            ImGui.End();
        }
    }
}
