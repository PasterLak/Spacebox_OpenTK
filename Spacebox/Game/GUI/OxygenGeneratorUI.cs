using Engine.Graphics.Atlas;
using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Oxygen;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class OxygenGeneratorUI
{
    private const string WindowId = "oxygenGenerator";
    private const int TankSteps = 3;

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 WaterColor = new Vector4(0.15f, 0.55f, 1f, 1f);
    private static readonly Vector4 TankBackground = new Vector4(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Vector4 NoPowerColor = new Vector4(1f, 0f, 0f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.6f, 0f, 1f);
    private static readonly Vector4 WorkingColor = new Vector4(0f, 1f, 0.4f, 1f);

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;

    private static OxygenGeneratorBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "Oxygen Generator";
    private static bool _wasVisible;

    public static void Open(OxygenGeneratorBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.WaterStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.WaterStorage);
        player.Panel.ConnectStorage(block.WaterStorage);

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
        float windowWidth = displaySize.Y * 0.4f;
        float windowHeight = displaySize.Y * 0.28f;
        float padding = windowHeight / 15f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("OxygenGenerator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                       ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _blockName);

        float contentTop = padding * 3f;
        float tankHeight = windowHeight - contentTop - padding;
        float tankWidth = slotSize * 0.6f;
        var tankMin = windowPos + new Vector2(padding, contentTop);
        float labelsWidth = DrawTank(tankMin, new Vector2(tankWidth, tankHeight));

        float rightX = padding * 2f + tankWidth + labelsWidth + padding;
        ImGui.SetCursorPos(new Vector2(rightX, contentTop));
        var slot = _block.WaterStorage.GetSlot(0, 0);
        InventoryUIHelper.DrawSlot(slot, "oxygenGeneratorWater", _onSlotClicked, GetWaterIcon());
        InventoryUIHelper.ShowTooltip(slot, false, true);

        ImGui.SetCursorPos(new Vector2(rightX, contentTop + slotSize + padding));
        ImGui.Text($"Tank: {_block.Water}/{_block.TankCapacity}");
        DrawStatus(rightX);

        ImGui.End();
    }

    private static float DrawTank(Vector2 min, Vector2 size)
    {
        var drawList = ImGui.GetWindowDrawList();
        var max = min + size;
        float fill = _block.TankCapacity > 0 ? (float)_block.Water / _block.TankCapacity : 0f;

        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(TankBackground));
        drawList.AddRectFilled(new Vector2(min.X, max.Y - size.Y * fill), max, ImGui.ColorConvertFloat4ToU32(WaterColor));
        drawList.AddRect(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.7f, 1f)));

        uint labelColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9f, 0.9f, 0.9f, 1f));
        float tickLength = size.X * 0.25f;
        float textHeight = ImGui.GetTextLineHeight();
        float labelsWidth = 0f;

        for (int i = 0; i <= TankSteps; i++)
        {
            int value = (int)MathF.Round(_block.TankCapacity * i / (float)TankSteps);
            float y = max.Y - size.Y * i / TankSteps;
            string label = value.ToString();

            drawList.AddLine(new Vector2(max.X, y), new Vector2(max.X + tickLength, y), labelColor);
            float labelY = Math.Clamp(y - textHeight * 0.5f, min.Y, max.Y - textHeight);
            drawList.AddText(new Vector2(max.X + tickLength * 1.5f, labelY), labelColor, label);

            labelsWidth = MathF.Max(labelsWidth, tickLength * 1.5f + ImGui.CalcTextSize(label).X);
        }

        return labelsWidth;
    }

    private static void DrawStatus(float x)
    {
        ImGui.SetCursorPosX(x);

        if (!_block.IsActive)
        {
            ImGui.TextColored(NoPowerColor, "NO POWER");
            return;
        }

        if (!_block.HasWater)
        {
            ImGui.TextColored(WarningColor, "No water");
            return;
        }

        if (_block.IsOutside)
        {
            ImGui.PushTextWrapPos(ImGui.GetWindowWidth() * 0.95f);
            ImGui.TextColored(WarningColor, "Room not sealed");
            ImGui.SetCursorPosX(x);
            ImGui.TextDisabled("Air leaks out");
            ImGui.PopTextWrapPos();
            return;
        }

        ImGui.TextColored(WorkingColor, "Working");
        ImGui.SetCursorPosX(x);
        float secondsLeft = (1f - _block.ConsumeProgress) * _block.WaterConsumeInterval;
        ImGui.TextDisabled($"Next water use: {secondsLeft:0}s");
    }

    private static AtlasSprite GetWaterIcon()
    {
        return UIAtlas.Get("water");
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

        _block?.WaterStorage.DisconnectStorage();
        _block = null;
    }
}
