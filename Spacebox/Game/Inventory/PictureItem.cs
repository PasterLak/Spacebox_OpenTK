using System.Text;
using Engine;
using Spacebox.Game.Pictures;

namespace Spacebox.Game;

public class PictureItem : Item, IItemTooltip, IItemTooltipImage
{
    public string PicturePath { get; init; } = "";
    public int Width { get; init; } = 1;
    public int Height { get; init; } = 1;

    public PictureItem(string name, float modelDepth) : base(1, name, modelDepth)
    {
    }

    public Texture2D TooltipImage => PictureTextures.Get(this);
    public float TooltipImageAspect => Width / (float)Height;

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nPicture {Width}x{Height}, hang it in a picture frame");
    }
}
