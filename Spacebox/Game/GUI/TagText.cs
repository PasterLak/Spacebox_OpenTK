using ImGuiNET;
using System.Numerics;
using Engine;
namespace Spacebox.Game.GUI
{
    public static class TagText
    {

        private static string _text = "";

        public static bool IsVisible { get; private set; } = true;

        private static Vector4 _color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);


        public static void SetText(string text)
        {
            _text = text;
        }


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


        public static void Draw()
        {

            if (!IsVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("CenteredTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);


            Vector2 textSize = ImGui.CalcTextSize(_text);

            Vector2 pos = Camera.Main.WorldToScreenPoint(Vector3.Zero.ToOpenTKVector3(),
                (int)ImGui.GetIO().DisplaySize.X,
               (int)ImGui.GetIO().DisplaySize.Y).ToSystemVector2();


            //float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            //float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.5f;
            if (pos != Vector2.Zero)
            {
                ImGui.SetCursorPos(new Vector2(pos.X, pos.Y));

                ImGui.PushStyleColor(ImGuiCol.Text, _color);
                ImGui.TextUnformatted(_text);
                ImGui.PopStyleColor();
            }


            ImGui.End();
        }
    }
}
