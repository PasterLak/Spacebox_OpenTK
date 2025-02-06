using ImGuiNET;
using Spacebox.Game;
using System.Numerics;
using Engine;
namespace Spacebox.GUI
{
    public static class CenteredText
    {
        private static string _text = "Press RMB to use";

        public static bool IsVisible { get; private set;}
              
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

        public static void OnGUI()
        {
            if(!Settings.ShowInterface) return;
            if (!IsVisible)
            {


                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
                ImGui.Begin("CenteredTextWindow2", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);


                Vector2 textSize1 = ImGui.CalcTextSize("+");


                float posX1 = (ImGui.GetWindowWidth() - textSize1.X) * 0.5f;
                float posY1 = (ImGui.GetWindowHeight() - textSize1.Y) * 0.5f;

                ImGui.SetCursorPos(new Vector2(posX1, posY1));

                ImGui.PushStyleColor(ImGuiCol.Text, _color);
                ImGui.TextUnformatted("+");
                ImGui.PopStyleColor();

                ImGui.End();
            }

            if (!IsVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("CenteredTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);

        
            Vector2 textSize = ImGui.CalcTextSize(_text);

           
            float posX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            float posY = (ImGui.GetWindowHeight() - textSize.Y) * 0.5f;

            ImGui.SetCursorPos(new Vector2(posX, posY));

            ImGui.PushStyleColor(ImGuiCol.Text, _color);
            ImGui.TextUnformatted(_text);
            ImGui.PopStyleColor();

            ImGui.End();
        }
    }
}
