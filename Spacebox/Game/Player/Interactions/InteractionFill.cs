using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Player.Interactions;

public sealed class InteractionFill : InteractionAreaTool
{
    public const string PickHint = "Alt + click a block to choose what to fill with";

    public override void OnEnable()
    {
        base.OnEnable();
        ShowMessage(Tools.FillBlockId != 0 ? FillText(Tools.FillBlockId) : PickHint);
    }

    protected override bool HandleToolInput(SpaceEntity entity, Vector3i cell, Vector3i normal, Block aimedBlock)
    {
        if (!IsPickHeld() || !Input.IsMouseButtonDown(MouseButton.Left)) return false;
        if (aimedBlock == null || aimedBlock.IsAir) return true;

        var data = GameAssets.GetBlockDataById(aimedBlock.Id);
        if (data == null || data.IsMultiblock)
        {
            ShowMessage("Multi-cell blocks can't be used for filling");
            return true;
        }

        Tools.FillBlockId = aimedBlock.Id;
        ShowMessage(FillText(aimedBlock.Id));
        return true;
    }

    protected override void Confirm()
    {
        if (Tools.FillBlockId == 0)
        {
            ShowMessage(PickHint);
            return;
        }

        if (!Tools.HasFullSelection)
        {
            ShowMessage("Select two corners first: LMB and RMB");
            return;
        }

        Tools.FillBlocks(Tools.FillBlockId);
    }

    private static bool IsPickHeld() => Input.IsKey(Keys.LeftAlt) || Input.IsKey(Keys.RightAlt);

    private static string FillText(short blockId) => $"Fill with: {GameAssets.GetBlockDataById(blockId)?.Name ?? "?"}";
}
