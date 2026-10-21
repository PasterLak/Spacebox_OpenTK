using Engine;

namespace Spacebox.Game;

public interface IItemTooltipImage
{
    Texture2D TooltipImage { get; }
    float TooltipImageAspect { get; }
}
