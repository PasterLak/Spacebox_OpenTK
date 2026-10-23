using OpenTK.Mathematics;

namespace Spacebox.Game.GUI
{
    public static class ItemControlsUI
    {
        public static bool IsVisible { get; set; } = true;
        public static string Text { get; set; } = "";
        public static Color4 TextColor { get; set; } = new Color4(1f, 1f, 1f, 0.5f);
    }
}
