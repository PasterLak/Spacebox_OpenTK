using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class DnaReconstructorUI
{
    private const string WindowId = "dnaReconstructor";
    private const string SlotIcon = "dna_ui";

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 NoPowerColor = new Vector4(1f, 0f, 0f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.6f, 0f, 1f);
    private static readonly Vector4 ReadyColor = new Vector4(0f, 1f, 0.4f, 1f);

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;

    private static DnaReconstructorBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "DNA Reconstructor";
    private static bool _wasVisible;

    public static void Open(DnaReconstructorBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.DnaStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.DnaStorage);
        player.Panel.ConnectStorage(block.DnaStorage);

        if (player.GameMode != GameMode.Survival)
            UIManager.Open(WindowId, "inventory", "creative");
        else
            UIManager.Open(WindowId, "inventory");
    }

    public static void OnGUI()
    {
        bool isVisible = UIManager.IsOpen(WindowId) && _block != null;
        if (_wasVisible && !isVisible) OnClosed();
        _wasVisible = isVisible;
        if (!isVisible) return;

        if (Input.IsKeyDown(Keys.Escape))
        {
            UIManager.CloseAll();
            return;
        }

        var displaySize = ImGui.GetIO().DisplaySize;
        float slotSize = InventoryUIHelper.SlotSize;
        float windowWidth = displaySize.Y * 0.42f;
        float windowHeight = displaySize.Y * 0.26f;
        float padding = windowHeight / 15f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("DnaReconstructor", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                        ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _blockName);

        float contentTop = padding * 3f;
        ImGui.SetCursorPos(new Vector2(padding, contentTop));
        var slot = _block.DnaStorage.GetSlot(0, 0);
        InventoryUIHelper.DrawSlot(slot, "dnaReconstructorSlot", _onSlotClicked, UIAtlas.Get(SlotIcon));
        InventoryUIHelper.ShowTooltip(slot, false, true);

        float textX = padding * 2f + slotSize;
        ImGui.SetCursorPos(new Vector2(textX, contentTop));
        ImGui.Text($"DNA samples: {_block.DnaCount}/{_block.DnaCapacity}");
        ImGui.SetCursorPosX(textX);
        ImGui.Text($"Power: {_block.Data.Power} EU");
        DrawStatus(textX);

        ImGui.SetCursorPos(new Vector2(padding, contentTop + slotSize + padding));
        ImGui.PushTextWrapPos(windowWidth - padding);
        ImGui.TextDisabled("After death you can be reconstructed at the nearest powered reconstructor. Uses 1 DNA sample.");
        ImGui.PopTextWrapPos();

        ImGui.End();
    }

    private static void DrawStatus(float x)
    {
        ImGui.SetCursorPosX(x);

        if (!_block.IsActive)
        {
            ImGui.TextColored(NoPowerColor, "NO POWER");
            return;
        }

        if (!_block.HasDna)
        {
            ImGui.TextColored(WarningColor, "No DNA sample");
            return;
        }

        ImGui.TextColored(ReadyColor, "Ready");
    }

    private static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    public static void Reset()
    {
        _block = null;
        _player = null;
        _wasVisible = false;
    }

    private static void OnClosed()
    {
        if (_player != null)
        {
            _player.Panel.ConnectStorage(_player.Inventory, true);
            _player.Inventory.ConnectStorage(_player.Panel);
        }

        _block?.DnaStorage.DisconnectStorage();
        _block = null;
    }
}
