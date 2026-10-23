using System.Numerics;
using Engine.UI;
using Spacebox.Game.Player;
using Spacebox.GUI;


namespace Spacebox.Game.GUI
{

    public class StatsGUI
    {
        public bool IsVisible { get; set; } = true;
        public StatsData StatsData { get; set; }
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
        private Vector2 _lastDisplaySize;
        public string TextOverride { get; set; }
        public Vector2 Size { get; set; } = new Vector2(200, 50);
        public StatsGUI(StatsData statsData)
        {
            StatsData = statsData;
            WindowName = $"{statsData.Name}Bar";
            _position = Position;
            //_size = CalculateSize(Size,Window.Instance.Size);
            //_position = CalculateSize(Position, Window.Instance.Size);

            OnResized(SpaceboxWindow.Instance.Size);
        }

        public void OnResized(OpenTK.Mathematics.Vector2 w)
        {

            _size = GUIHelper.CalculateSize(Size, w);
            _position = GUIHelper.CalculateSize(Position, w);
        }
    }
}
