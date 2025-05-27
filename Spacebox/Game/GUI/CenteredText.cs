using ImGuiNET;
using Spacebox.Game;
using System.Numerics;
using Engine;
namespace Spacebox.GUI
{
    public static class CenteredText
    {
        private static string _text = "Press RMB to use";

        public static bool IsVisible { get; private set; }

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

        public enum HorizontalAlign
        {
            Left,
            Center,
            Right
        }

        public enum VerticalAlign
        {
            BlockCenter,     
            FirstLineCenter,  
            LastLineCenter   
        }

        private static void DrawMultiline(
            string text,
            Vector4 color,
            HorizontalAlign hAlign = HorizontalAlign.Center,
            VerticalAlign vAlign = VerticalAlign.BlockCenter)
        {
           
            string[] lines = text.Split('\n');
            float lineHeight = ImGui.GetTextLineHeightWithSpacing();
            float blockHeight = lines.Length * lineHeight;

            float winW = ImGui.GetWindowWidth();
            float winH = ImGui.GetWindowHeight();
            float centerY = winH * 0.5f;

        
            float startY;
            switch (vAlign)
            {
                case VerticalAlign.BlockCenter:
                    startY = (winH - blockHeight) * 0.5f;
                    break;
                case VerticalAlign.FirstLineCenter:
                    startY = centerY - lineHeight * 0.5f;
                    break;
                case VerticalAlign.LastLineCenter:
                    startY = centerY - lineHeight * 0.5f - (lines.Length - 1) * lineHeight;
                    break;
                default:
                    startY = 0;
                    break;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 size = ImGui.CalcTextSize(lines[i]);

                float startX;
                switch (hAlign)
                {
                    case HorizontalAlign.Left:
                        startX = 0f;
                        break;
                    case HorizontalAlign.Right:
                        startX = winW - size.X;
                        break;
                    case HorizontalAlign.Center:
                    default:
                        startX = (winW - size.X) * 0.5f;
                        break;
                }

                ImGui.SetCursorPos(new Vector2(startX, startY + i * lineHeight));
                ImGui.TextUnformatted(lines[i]);
            }

            ImGui.PopStyleColor();
        }


        public static void OnGUI()
        {
            if (!Settings.ShowInterface) return;
            if (!IsVisible)
            {


                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
                ImGui.Begin("CenteredTextWindow2", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);


                DrawMultiline("+", _color, HorizontalAlign.Center, VerticalAlign.BlockCenter);

                ImGui.End();
            }

            if (!IsVisible)
                return;

            ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always, new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y));
            ImGui.Begin("CenteredTextWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);



            ImGui.PushStyleColor(ImGuiCol.Text, _color);

            DrawMultiline(_text, _color, HorizontalAlign.Center, VerticalAlign.FirstLineCenter);

            ImGui.PopStyleColor();

            ImGui.End();
        }
    }
}
