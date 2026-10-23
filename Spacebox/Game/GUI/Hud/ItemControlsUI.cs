using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public sealed class ItemControlsUI : IGameUiController
    {
        public bool IsVisible { get; set; } = true;
        public string Text { get; set; } = "";
        public Color4 TextColor { get; set; } = new Color4(1f, 1f, 1f, 0.5f);
    }
}
