using System.Numerics;
using ImGuiNET;
using Spacebox.Engine;
using Spacebox.Game.Player;
using Spacebox.GUI;

namespace Spacebox.Game.GUI
{

    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
     
        Center,
        Top,
        Bottom,
        Left,
        Right


    }
    public class StatsGUI
    {
        public StatsBarData StatsData { get; set; }
        private Vector2 _size { get; set; } = new Vector2(200, 50);
        private Vector2 _position { get; set; } = new Vector2(0, 0);
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        private Vector2 basePosition { get; set; } = new Vector2(0, 0);
        public Vector4 FillColor { get; set; } = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        public Vector4 BackgroundColor { get; set; } = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
        public Vector4 TextColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);
        public Anchor Anchor { get; set; } = Anchor.TopLeft;
        public string WindowName { get; set; } = "StatsBar";
        public bool ShowText = true;
        public Vector2 Size { get; set; } = new Vector2(200, 50);
        public StatsGUI(StatsBarData statsData)
        {
            StatsData = statsData;
            WindowName = $"{statsData.Name}Bar";
            _position = Position;
            //_size = CalculateSize(Size,Window.Instance.Size);
            //_position = CalculateSize(Position, Window.Instance.Size);

            Window.OnResized += OnResized;

            OnResized(Window.Instance.Size);
        }

        ~StatsGUI()
        {
            Window.OnResized -= OnResized;
        }

        public void OnResized(OpenTK.Mathematics.Vector2 w)
        {
          
            _size = GUIHelper.CalculateSize(Size,w);
            _position = GUIHelper.CalculateSize(Position, w);
        }
        
        public void OnGUI()
        {
            if (!Settings.ShowInterface) return;

          
            var io = ImGui.GetIO();
            switch (Anchor)
            {
                case Anchor.TopLeft:
                    basePosition = new Vector2(0, 0) + _position;
                    break;
                case Anchor.TopRight:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X, 0) 
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.BottomLeft:
                    basePosition = new Vector2(0, io.DisplaySize.Y - _size.Y)
                         + new Vector2(_position.X, -_position.Y);
                    break;
                case Anchor.BottomRight:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X, io.DisplaySize.Y - _size.Y) - _position;
                    break;
               

                case Anchor.Right:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X,
                        io.DisplaySize.Y * 0.5f - _size.Y * 0.5f)
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.Left:
                    basePosition = new Vector2(0, 
                        io.DisplaySize.Y * 0.5f - _size.Y * 0.5f)
                        + new Vector2(_position.X, _position.Y);
                    break;
                case Anchor.Top:
                    basePosition = new Vector2(io.DisplaySize.X * 0.5f - _size.X * 0.5f, 0)
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.Bottom:
                    basePosition = new Vector2(io.DisplaySize.X * 0.5f - _size.X * 0.5f, io.DisplaySize.Y - _size.Y) 
                        +new Vector2(_position.X,  - _position.Y); 
                    break;
             
                default:
                    break;
            }
            
            ImGui.SetNextWindowPos(basePosition, ImGuiCond.Always);
            ImGui.SetNextWindowSize(_size, ImGuiCond.Always);

            ImGui.Begin(WindowName, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                         ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing |
                                         ImGuiWindowFlags.NoInputs);

            float fillPercent = (float)StatsData.Count / StatsData.MaxCount;
            fillPercent = Math.Clamp(fillPercent, 0f, 1f);

            ImGui.GetWindowDrawList().AddRectFilled(
                basePosition,
                basePosition + _size,
                ImGui.ColorConvertFloat4ToU32(BackgroundColor)
            );

            ImGui.GetWindowDrawList().AddRectFilled(
                basePosition,
                new Vector2(basePosition.X + _size.X * fillPercent, basePosition.Y + _size.Y),
                ImGui.ColorConvertFloat4ToU32(FillColor)
            );

            string text = $"{StatsData.Count}/{StatsData.MaxCount}";
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = basePosition + (_size - textSize) / 2;

            if(ShowText)
            {
                ImGui.GetWindowDrawList().AddText(
                textPos,
                ImGui.ColorConvertFloat4ToU32(TextColor),
                text
            );
            }
            

            ImGui.End();
        }
    }
}
