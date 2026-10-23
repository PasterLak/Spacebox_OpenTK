using Engine;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Player.Building;
using Spacebox.Network;

namespace Spacebox.Game.Player.Interactions;

public sealed class InteractionCopyPaste : InteractionAreaTool
{
    private const string SelectHint = "Select an area with LMB and RMB, Enter copies it";
    private const string PasteHint = "LMB pastes, R rotates, RMB selects a new area";

    private static readonly Color4 PreviewColor = new Color4(0.35f, 0.95f, 0.45f, 1f);

    private EntityBoxOutline _preview;
    private bool _pasting;

    protected override bool ShowsSelection => !_pasting;

    public override void OnEnable()
    {
        base.OnEnable();
        _preview = new EntityBoxOutline(PreviewColor, 0.1f, 0.04f);
        _pasting = CreativeTools.Clipboard != null;
        ShowMessage(_pasting ? PasteHint : SelectHint);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        _preview?.Dispose();
        _preview = null;
    }

    protected override bool HandleToolInput(SpaceEntity entity, Vector3i cell, Vector3i normal, Block aimedBlock)
    {
        var clipboard = CreativeTools.Clipboard;
        if (!_pasting || clipboard == null)
        {
            _pasting = false;
            return false;
        }

        if (Input.IsActionDown("block_rotate")) clipboard.Turn(BaseFrontOf);

        var min = BlockClipboard.PasteMin(cell + normal, normal, clipboard.Size);
        var max = min + clipboard.Size - Vector3i.One;
        _preview.Show(entity, min, max);

        if (Input.IsMouseButtonDown(MouseButton.Left))
        {
            if (Spacebox.Game.Protection.EntityProtection.CheckLocalEditBox(entity, min, max) && WorldEditNetwork.RequestPaste(entity, min, clipboard))
                ShowMessage($"Pasted {clipboard.Count} blocks");
        }
        else if (Input.IsMouseButtonDown(MouseButton.Right) || Input.IsMouseButtonDown(MouseButton.Middle))
        {
            _pasting = false;
            _preview.Hide();
            CreativeTools.Reset();
            ShowMessage(SelectHint);
        }
        return true;
    }

    protected override void OnNoTarget()
    {
        _preview?.Hide();
    }

    protected override void Confirm()
    {
        if (!CreativeTools.HasFullSelection || !CreativeTools.TryGetSelection(out var entity, out var min, out var max))
        {
            ShowMessage(SelectHint);
            return;
        }

        var clipboard = BlockPaste.Capture(entity, min, max, out var error);
        if (clipboard == null)
        {
            ShowMessage(error);
            return;
        }

        CreativeTools.Clipboard = clipboard;
        CreativeTools.Reset();
        _pasting = true;
        ShowMessage($"Copied {clipboard.Count} blocks. {PasteHint}");
    }

    protected override void RenderTool()
    {
        if (_pasting) _preview?.Render();
    }

    private static Direction BaseFrontOf(short blockId) => GameAssets.GetBlockDataById(blockId)?.BaseFrontDirection ?? Direction.Up;
}
