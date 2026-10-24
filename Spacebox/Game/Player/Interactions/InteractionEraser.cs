namespace Spacebox.Game.Player.Interactions;

public class InteractionEraser : InteractionAreaTool
{
    protected override void Confirm() => Tools.DeleteBlocks();
}
