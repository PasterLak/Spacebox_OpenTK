using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Game.GUI.Canvas.Items;

public static class ItemTooltipPicture
{
    public const float MinAspect = 0.25f;
    public const float MaxAspect = 4f;

    public static void Apply(Control control, Item item, float height)
    {
        if (item is IItemTooltipImage picture && picture.TooltipImage != null)
        {
            control.TooltipImage = picture.TooltipImage;
            control.TooltipImageFlipV = true;
            height = MathF.Round(height);
            control.TooltipImageSize = new Vector2(MathF.Round(height * Math.Clamp(picture.TooltipImageAspect, MinAspect, MaxAspect)), height);
            return;
        }

        control.TooltipImage = null;
        control.TooltipImageFlipV = false;
        control.TooltipImageSize = Vector2.Zero;
    }
}
