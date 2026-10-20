using System.Numerics;
using Engine;
using Engine.Audio;
using Engine.Graphics.Atlas;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Disassembly;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.GUI;

public static class DisassemblerUI
{
    public const string WindowId = "disassembler";

    private const float WindowWidth = 0.66f;
    private const float WindowHeight = 0.27f;
    private const float Padding = 0.018f;
    private const float TextHeight = 0.02f;
    private const float StatusTop = 0.045f;
    private const float GridsTop = 0.08f;
    private const float SlotGap = 0.006f;
    private const float ArrowMargin = 0.015f;
    private const float ArrowHeight = 0.05f;
    private const float PreviewGap = 0.012f;
    private const float PreviewIcon = 0.03f;

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 InfoColor = new Vector4(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Vector4 NoPowerColor = new Vector4(1f, 0.2f, 0.2f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.6f, 0f, 1f);
    private static readonly Vector4 WorkingColor = new Vector4(0f, 1f, 0.4f, 1f);
    private static readonly Vector4 IdleColor = new Vector4(0.95f, 0.95f, 0.95f, 1f);
    private static readonly Vector4 ArrowBackground = new Vector4(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Vector4 ArrowFill = new Vector4(0.95f, 0.55f, 0.15f, 1f);
    private static readonly Vector4 ArrowBorder = new Vector4(0.7f, 0.7f, 0.7f, 1f);

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;
    private static readonly Action<DisassemblerBlock> _onDisassembled = OnDisassembled;
    private static readonly Vector2[] _arrowOutline = new Vector2[7];

    private static DisassemblerBlock _block;
    private static LocalAstronaut _player;
    private static string _blockName = "Disassembler";
    private static bool _wasVisible;
    private static AudioSource _craftedSound;

    public static void Open(DisassemblerBlock block, LocalAstronaut player)
    {
        if (_block != null) _block.OnDisassembled -= _onDisassembled;

        _block = block;
        _player = player;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;
        block.ResetSyncedState();
        block.OnDisassembled += _onDisassembled;

        if (_craftedSound == null || _craftedSound.IsDisposed)
            _craftedSound = new AudioSource(Resources.Load<AudioClip>("crafted"), AudioChannel.UI);

        block.InputStorage.ConnectStorage(player.Inventory);
        block.OutputStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.InputStorage);
        player.Panel.ConnectStorage(block.InputStorage);

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

        float unit = ImGui.GetIO().DisplaySize.Y;
        float slotStep = InventoryUIHelper.SlotSize + SlotGap * unit;
        float gridWidth = slotStep * DisassemblerInventory.Width - SlotGap * unit;
        float gridHeight = slotStep * DisassemblerInventory.Height - SlotGap * unit;
        float padding = Padding * unit;

        var windowSize = new Vector2(WindowWidth, WindowHeight) * unit;
        var windowPos = GameMenu.CenterNextWindow3(windowSize.X, windowSize.Y);

        ImGui.Begin("Disassembler", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                    ImGuiWindowFlags.NoScrollWithMouse);

        ScaleFont(unit);
        GameMenu.DrawElementColors(windowPos, windowSize, unit);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _blockName);
        ImGui.SameLine(0f, padding);
        ImGui.TextColored(InfoColor, $"{_block.Power} EU");
        ImGui.SetCursorPos(new Vector2(padding, StatusTop * unit));
        DrawStatus();

        float gridsTop = GridsTop * unit;
        float outputX = windowSize.X - padding - gridWidth;
        DrawGrid(_block.InputStorage, "disassemblerInput", new Vector2(padding, gridsTop), slotStep, UIAtlas.Get("disassemblerinput"));
        DrawGrid(_block.OutputStorage, "disassemblerOutput", new Vector2(outputX, gridsTop), slotStep, UIAtlas.Get("furnaceoutput"));

        float arrowLeft = padding + gridWidth + ArrowMargin * unit;
        float arrowWidth = outputX - ArrowMargin * unit - arrowLeft;
        float arrowHeight = ArrowHeight * unit;
        var arrowMin = windowPos + new Vector2(arrowLeft, gridsTop + (gridHeight - arrowHeight) * 0.5f);
        DrawArrow(arrowMin, new Vector2(arrowWidth, arrowHeight), _block.HasPower ? _block.ProgressNormalized : 0f, unit);

        DrawPreview(windowPos + new Vector2(padding, gridsTop + gridHeight + PreviewGap * unit), PreviewIcon * unit);

        ImGui.SetWindowFontScale(1f);
        ImGui.End();
    }

    private static void ScaleFont(float unit)
    {
        ImGui.SetWindowFontScale(1f);
        float baseSize = ImGui.GetFontSize();
        if (baseSize > 0f) ImGui.SetWindowFontScale(TextHeight * unit / baseSize);
    }

    private static void DrawGrid(Storage storage, string id, Vector2 origin, float step, AtlasSprite background)
    {
        for (byte y = 0; y < storage.SizeY; y++)
        for (byte x = 0; x < storage.SizeX; x++)
        {
            var slot = storage.GetSlot(x, y);
            ImGui.SetCursorPos(origin + new Vector2(x * step, y * step));
            InventoryUIHelper.DrawSlot(slot, $"{id}_{x}_{y}", _onSlotClicked, background);
            InventoryUIHelper.ShowTooltip(slot, false, true);
        }
    }

    private static void DrawStatus()
    {
        if (!_block.HasPower)
        {
            ImGui.TextColored(NoPowerColor, "NO POWER");
            return;
        }

        switch (_block.Status)
        {
            case DisassemblerStatus.Working:
                ImGui.TextColored(WorkingColor, $"Working {(int)(_block.ProgressNormalized * 100f)}%");
                break;
            case DisassemblerStatus.OutputFull:
                ImGui.TextColored(WarningColor, "Output is full");
                break;
            case DisassemblerStatus.NotEnoughItems:
                ImGui.TextColored(WarningColor, "Not enough items");
                break;
            case DisassemblerStatus.CannotDisassemble:
                ImGui.TextColored(WarningColor, "Can't be disassembled");
                break;
            default:
                ImGui.TextColored(IdleColor, "Put items to disassemble");
                break;
        }
    }

    private static void DrawArrow(Vector2 min, Vector2 size, float fill, float unit)
    {
        var drawList = ImGui.GetWindowDrawList();
        float centerY = min.Y + size.Y * 0.5f;
        float shaftHalf = size.Y * 0.21f;
        float headX = min.X + size.X * 0.62f;
        var max = min + size;

        DrawArrowShape(drawList, min, max, centerY, shaftHalf, headX, ImGui.ColorConvertFloat4ToU32(ArrowBackground));

        if (fill > 0f)
        {
            drawList.PushClipRect(min, new Vector2(min.X + size.X * fill, max.Y), true);
            DrawArrowShape(drawList, min, max, centerY, shaftHalf, headX, ImGui.ColorConvertFloat4ToU32(ArrowFill));
            drawList.PopClipRect();
        }

        _arrowOutline[0] = new Vector2(min.X, centerY - shaftHalf);
        _arrowOutline[1] = new Vector2(headX, centerY - shaftHalf);
        _arrowOutline[2] = new Vector2(headX, min.Y);
        _arrowOutline[3] = new Vector2(max.X, centerY);
        _arrowOutline[4] = new Vector2(headX, max.Y);
        _arrowOutline[5] = new Vector2(headX, centerY + shaftHalf);
        _arrowOutline[6] = new Vector2(min.X, centerY + shaftHalf);
        drawList.AddPolyline(ref _arrowOutline[0], _arrowOutline.Length, ImGui.ColorConvertFloat4ToU32(ArrowBorder), ImDrawFlags.Closed, MathF.Max(1f, unit * 0.0015f));
    }

    private static void DrawArrowShape(ImDrawListPtr drawList, Vector2 min, Vector2 max, float centerY, float shaftHalf, float headX, uint color)
    {
        drawList.AddRectFilled(new Vector2(min.X, centerY - shaftHalf), new Vector2(headX, centerY + shaftHalf), color);
        drawList.AddTriangleFilled(new Vector2(headX, min.Y), new Vector2(max.X, centerY), new Vector2(headX, max.Y), color);
    }

    private static void DrawPreview(Vector2 min, float iconSize)
    {
        var item = GetPreviewItem();
        if (item == null || !DisassemblyRecipes.TryGet(item, out var recipe)) return;

        var drawList = ImGui.GetWindowDrawList();
        uint textColor = ImGui.ColorConvertFloat4ToU32(IdleColor);
        float textY = min.Y + (iconSize - ImGui.GetTextLineHeight()) * 0.5f;
        float x = min.X;

        x = DrawStack(drawList, recipe.Input, recipe.InputQuantity, new Vector2(x, min.Y), iconSize, textColor);
        drawList.AddText(new Vector2(x, textY), textColor, "->");
        x += ImGui.CalcTextSize("-> ").X;

        for (int i = 0; i < recipe.Products.Count; i++)
            x = DrawStack(drawList, recipe.Products[i].Item, recipe.Products[i].Quantity, new Vector2(x, min.Y), iconSize, textColor);

        drawList.AddText(new Vector2(x, textY), textColor, $"{Time.TicksToSeconds(recipe.DurationTicks):0.#}s");
    }

    private static float DrawStack(ImDrawListPtr drawList, Item item, int quantity, Vector2 min, float iconSize, uint textColor)
    {
        item.Icon.Draw(drawList, min, min + new Vector2(iconSize));
        string label = "x" + quantity;
        float textY = min.Y + (iconSize - ImGui.GetTextLineHeight()) * 0.5f;
        drawList.AddText(new Vector2(min.X + iconSize * 1.1f, textY), textColor, label);
        return min.X + iconSize * 1.1f + ImGui.CalcTextSize(label + "  ").X;
    }

    private static Item GetPreviewItem()
    {
        if (_block.CurrentItemId != 0 && GameAssets.TryGetItemById(_block.CurrentItemId, out var current)) return current;

        foreach (var slot in _block.InputStorage.GetAllSlots())
        {
            if (slot.HasItem) return slot.Item;
        }

        return null;
    }

    private static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    private static void OnDisassembled(DisassemblerBlock block)
    {
        if (block != _block || !UIManager.IsOpen(WindowId) || _craftedSound == null) return;

        if (_craftedSound.IsPlaying) _craftedSound.Stop();
        _craftedSound.Play();
    }

    public static void Reset()
    {
        if (_block != null) _block.OnDisassembled -= _onDisassembled;
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

        if (_block != null)
        {
            _block.OnDisassembled -= _onDisassembled;
            _block.InputStorage.DisconnectStorage();
            _block.OutputStorage.DisconnectStorage();
        }

        _block = null;
    }
}
