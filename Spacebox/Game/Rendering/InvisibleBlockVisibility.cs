using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Rendering;

public static class InvisibleBlockVisibility
{
    public static bool IsShown => !PanelUI.AreHandsBlocked && PanelUI.CurrentSlot()?.Item is BlockItem item && InvisibleBlock.IsInvisibleType(item.BlockId);
}
