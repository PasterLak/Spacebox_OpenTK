using System.Numerics;
using ImGuiNET;
using Spacebox.Common;
using Spacebox.Game;

namespace Spacebox.GUI
{
    public enum Anchor
    {
        Left,
        Right,
        Top, 
        Bottom, 
        TopLeft, BottomRight,
        DownLeft, DownRight
    }
    public class HealthBar
    {
        private readonly StatsBarData _statsData;
        private  Vector2 _size;
        private  Vector2 _position;

        private Anchor _anchor;
        public HealthBar()
        {
            _statsData = new StatsBarData
            {
                Count = 100,
                MaxCount = 100,
                Name = "Health"
            };
            _size = new Vector2(200, 50);
            _position = new Vector2(400, 550);

            _anchor = Anchor.TopLeft;

            if(_anchor == Anchor.TopLeft)
            {
                _position = new Vector2(0,0);
            }
        }

        public void OnGUI()
        {
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.U))
            {
                _statsData.Decrement(2);
            }

            var io = ImGui.GetIO();

            _position = new Vector2(io.DisplaySize.X / 4, io.DisplaySize.Y - io.DisplaySize.Y / 6);
            _size =  new Vector2(io.DisplaySize.X / 1920 * 200, io.DisplaySize.Y / 1080 * 50);

            ImGui.SetNextWindowPos(_position, ImGuiCond.Always);
            ImGui.SetNextWindowSize(_size, ImGuiCond.Always);

            ImGui.Begin("HealthBar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground |
                                         ImGuiWindowFlags.NoInputs);

            float fillPercent = (float)_statsData.Count / _statsData.MaxCount;
            fillPercent = Math.Clamp(fillPercent, 0f, 1f);

            Vector4 backgroundColor = new Vector4(0.2f, 0.2f, 0.2f, _statsData.Count > 0 ? 1.0f : 0.0f);
            Vector4 fillColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

            ImGui.GetWindowDrawList().AddRectFilled(
                _position,
                new Vector2(_position.X + _size.X, _position.Y + _size.Y),
                ImGui.ColorConvertFloat4ToU32(backgroundColor)
            );

            ImGui.GetWindowDrawList().AddRectFilled(
                _position,
                new Vector2(_position.X + _size.X * fillPercent, _position.Y + _size.Y),
                ImGui.ColorConvertFloat4ToU32(fillColor)
            );

            string text = $"{_statsData.Count}/{_statsData.MaxCount}";
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = _position + (_size - new Vector2(textSize.X, textSize.Y)) / 2;

            ImGui.GetWindowDrawList().AddText(
                new Vector2(textPos.X, textPos.Y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)),
                text
            );

            ImGui.End();
        }
    }
}
