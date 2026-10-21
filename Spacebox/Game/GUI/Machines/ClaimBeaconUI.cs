using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Commands;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Protection;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ClaimBeaconUI
{
    private const string WindowId = "claimBeacon";

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 SecondaryColor = new Vector4(0.3f, 0.3f, 0.33f, 1f);
    private static readonly Vector4 ActiveColor = new Vector4(0f, 1f, 0.4f, 1f);
    private static readonly Vector4 InactiveColor = new Vector4(1f, 0f, 0f, 1f);
    private static readonly Vector4 BarBackground = new Vector4(0.45f, 0.45f, 0.48f, 1f);
    private static readonly Vector4 BarFill = new Vector4(0f, 0.8f, 0.35f, 1f);
    private static readonly Vector4 BarLow = new Vector4(1f, 0.6f, 0f, 1f);
    private static readonly Vector4 InputBackground = new Vector4(0.88f, 0.88f, 0.9f, 1f);
    private static readonly Vector4 InputText = new Vector4(0.1f, 0.1f, 0.12f, 1f);
    private static readonly Vector4 InputHint = new Vector4(0.42f, 0.42f, 0.46f, 1f);

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;
    private static readonly List<KeyValuePair<string, int>> _sortedPayments = new List<KeyValuePair<string, int>>();
    private static ClaimSettings _sortedFor;

    private static ClaimBeaconBlock _block;
    private static LocalAstronaut _player;
    private static bool _wasVisible;
    private static string _newMember = "";
    private const int MaxNameSuggestions = 6;
    private static readonly List<string> _onlinePlayers = new List<string>();
    private static readonly List<string> _excludedNames = new List<string>();
    private static readonly List<string> _nameSuggestions = new List<string>();
    private static string _blockName = "Claim Beacon";

    public static bool ShowZone { get; private set; } = true;
    public static ClaimBeaconBlock OpenBlock => UIManager.IsOpen(WindowId) ? _block : null;

    public static void Open(ClaimBeaconBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _newMember = "";
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.PaymentStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.PaymentStorage);
        player.Panel.ConnectStorage(block.PaymentStorage);

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
        float slot = InventoryUIHelper.SlotSize;
        float gap = displaySize.Y * 0.015f;
        float inventoryTop = InventoryUI.WindowSize.Y > 0f ? InventoryUI.WindowPosition.Y : displaySize.Y * 0.55f;

        float width = InventoryUI.WindowSize.X > 0f ? InventoryUI.WindowSize.X : displaySize.Y * 0.62f;
        var windowX = InventoryUI.WindowSize.X > 0f ? InventoryUI.WindowPosition.X : (displaySize.X - width) * 0.5f;
        float height = MathF.Min(displaySize.Y * 0.25f, inventoryTop - gap * 3f);
        var windowPos = new Vector2(windowX, inventoryTop - gap - height);
        var windowSize = new Vector2(width, height);

        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(windowSize);
        ImGui.Begin("ClaimBeacon", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration |
                                   ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        float padding = height / 15f;
        float inner = width - padding * 4f;
        float leftWidth = inner * 0.38f;
        float middleWidth = inner * 0.2f;
        float rightWidth = inner - leftWidth - middleWidth;

        var leftMin = new Vector2(padding, padding);
        var middleMin = new Vector2(leftMin.X + leftWidth + padding, padding);
        var rightMin = new Vector2(middleMin.X + middleWidth + padding, padding);

        DrawStatusColumn(windowPos, leftMin, leftWidth, slot);
        DrawDivider(windowPos, middleMin.X - padding * 0.5f, padding, height - padding);
        DrawPaymentsColumn(middleMin, middleWidth, height - padding * 2f, slot);
        DrawDivider(windowPos, rightMin.X - padding * 0.5f, padding, height - padding);
        DrawMembersColumn(rightMin, rightWidth, height - padding * 2f);

        ImGui.End();
    }

    public static void Reset()
    {
        _block = null;
        _player = null;
        _wasVisible = false;
        _newMember = "";
    }

    private static void DrawStatusColumn(Vector2 windowPos, Vector2 min, float width, float slotSize)
    {
        var settings = ClaimSettings.Current;

        ImGui.SetCursorPos(min);
        Text(TitleColor, _blockName);
        ImGui.SetCursorPosX(min.X);
        ImGui.TextUnformatted($"Owner: {(_block.Owner.Length > 0 ? _block.Owner : "nobody")}");

        ImGui.SetCursorPosX(min.X);
        if (!settings.Enabled)
        {
            Text(InactiveColor, "Claims are disabled");
            return;
        }

        if (_block.IsClaimActive) Text(ActiveColor, $"Protected: {ClaimCost.FormatDuration(_block.RemainingSeconds)}");
        else Text(InactiveColor, "Not protected");

        ImGui.SetCursorPosX(min.X);
        DrawTimeBar(windowPos, width, settings);

        ImGui.SetCursorPosX(min.X);
        int size = settings.Radius * 2 + 1;
        ImGui.PushTextWrapPos(min.X + width);
        Text(SecondaryColor, _block.CoversWholeEntity ? "Area: whole ship" : $"Area: {size}x{size}x{size}");
        ImGui.PopTextWrapPos();

        ImGui.SetCursorPosX(min.X);
        var slot = _block.PaymentStorage.GetSlot(0, 0);
        InventoryUIHelper.DrawSlot(slot, "claimBeaconPayment", _onSlotClicked);
        InventoryUIHelper.ShowTooltip(slot, false, true);
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (slotSize - ImGui.GetTextLineHeight() * 2f) * 0.5f);
        ImGui.PushTextWrapPos(min.X + width);
        ImGui.TextUnformatted("Pay with ingots");
        ImGui.PopTextWrapPos();
    }

    private static void DrawTimeBar(Vector2 windowPos, float width, ClaimSettings settings)
    {
        var drawList = ImGui.GetWindowDrawList();
        var start = windowPos + ImGui.GetCursorPos();
        float barHeight = ImGui.GetTextLineHeight() * 0.6f;
        float fill = settings.MaxStoredSeconds > 0 ? Math.Clamp(_block.RemainingSeconds / settings.MaxStoredSeconds, 0f, 1f) : 0f;
        var color = _block.RemainingSeconds <= settings.WarningSeconds ? BarLow : BarFill;

        drawList.AddRectFilled(start, start + new Vector2(width, barHeight), ImGui.ColorConvertFloat4ToU32(BarBackground));
        drawList.AddRectFilled(start, start + new Vector2(width * fill, barHeight), ImGui.ColorConvertFloat4ToU32(color));
        drawList.AddRect(start, start + new Vector2(width, barHeight), Theme.Colors.BorderDarkUint);
        ImGui.Dummy(new Vector2(width, barHeight));

        if (!ImGui.IsItemHovered()) return;
        ImGui.BeginTooltip();
        ImGui.TextUnformatted($"Stores up to {ClaimCost.FormatDuration(settings.MaxStoredSeconds)}");
        if (settings.ExtraClaimCostPercent > 0) ImGui.TextUnformatted($"Each extra claim you own uses time {settings.ExtraClaimCostPercent}% faster");
        ImGui.EndTooltip();
    }

    private static void DrawPaymentsColumn(Vector2 min, float width, float height, float slotSize)
    {
        float iconSize = slotSize * 0.45f;
        ImGui.SetCursorPos(min);
        ImGui.TextUnformatted("Payment");

        ImGui.SetCursorPosX(min.X);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.BeginChild("claimPayments", new Vector2(width, height - ImGui.GetTextLineHeightWithSpacing()));

        var drawList = ImGui.GetWindowDrawList();
        var payments = SortedPayments();
        for (int i = 0; i < payments.Count; i++)
        {
            var payment = payments[i];
            var iconPos = ImGui.GetCursorScreenPos();
            bool hasItem = ItemLookup.TryFind(payment.Key, out var item);
            if (hasItem && item.Icon.IsValid) item.Icon.Draw(drawList, iconPos, iconPos + new Vector2(iconSize, iconSize));
            ImGui.Dummy(new Vector2(iconSize, iconSize));
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(hasItem ? item.Name : payment.Key);

            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (iconSize - ImGui.GetTextLineHeight()) * 0.5f);
            ImGui.TextUnformatted($"+{ClaimCost.FormatDuration(payment.Value * 60f)}");
        }

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static List<KeyValuePair<string, int>> SortedPayments()
    {
        var settings = ClaimSettings.Current;
        if (settings == _sortedFor && settings.Payments.Count == _sortedPayments.Count) return _sortedPayments;

        _sortedFor = settings;
        _sortedPayments.Clear();
        foreach (var payment in settings.Payments) _sortedPayments.Add(payment);
        _sortedPayments.Sort((a, b) => a.Value.CompareTo(b.Value));
        return _sortedPayments;
    }

    private static void DrawMembersColumn(Vector2 min, float width, float height)
    {
        bool canManage = ClaimMembersNetwork.CanManage(_block, GameSession.PlayerName, EntityProtection.LocalCanEdit);
        float line = ImGui.GetFrameHeightWithSpacing();

        ImGui.SetCursorPos(min);
        ImGui.TextUnformatted($"Members {_block.Members.Count}/{ClaimSettings.Current.MaxMembers}");

        float footer = canManage ? line * 3f : line;
        float listHeight = MathF.Max(line, height - ImGui.GetTextLineHeightWithSpacing() - footer);
        ImGui.SetCursorPosX(min.X);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.BeginChild("claimMembers", new Vector2(width, listHeight));
        if (_block.Members.Count == 0) Text(SecondaryColor, "No members yet");

        for (int i = 0; i < _block.Members.Count; i++)
        {
            string member = _block.Members[i];
            ImGui.TextUnformatted(member);
            if (!canManage) continue;

            ImGui.SameLine(width - ImGui.CalcTextSize("x").X - ImGui.GetStyle().FramePadding.X * 3f);
            if (ImGui.SmallButton($"x##{i}")) ClaimMembersNetwork.Request(_block, member, false);
        }
        ImGui.EndChild();
        ImGui.PopStyleColor();

        if (canManage)
        {
            ImGui.SetCursorPosX(min.X);
            float buttonWidth = ImGui.CalcTextSize("Add").X + ImGui.GetStyle().FramePadding.X * 4f;
            ImGui.SetNextItemWidth(width - buttonWidth - ImGui.GetStyle().ItemSpacing.X);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, InputBackground);
            ImGui.PushStyleColor(ImGuiCol.Text, InputText);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, InputHint);
            bool submit = ImGui.InputTextWithHint("##newMember", "Player name", ref _newMember, (uint)ClaimSettings.MaxNameLength, ImGuiInputTextFlags.EnterReturnsTrue);
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            if ((ImGui.Button("Add", new Vector2(buttonWidth, 0f)) || submit) && _newMember.Trim().Length > 0)
            {
                ClaimMembersNetwork.Request(_block, _newMember, true);
                _newMember = "";
            }

            DrawNameSuggestions(min.X, width);
        }

        ImGui.SetCursorPosX(min.X);
        bool showZone = ShowZone;
        if (ImGui.Checkbox("Show area", ref showZone)) ShowZone = showZone;
    }

    private static void DrawNameSuggestions(float x, float width)
    {
        _onlinePlayers.Clear();
        CommandSuggestionSources.AddPlayerNames(_onlinePlayers);
        _excludedNames.Clear();
        _excludedNames.AddRange(_block.Members);
        _excludedNames.Add(_block.Owner);
        _excludedNames.Add(GameSession.PlayerName);
        NameSuggestions.Filter(_newMember, _onlinePlayers, _excludedNames, _nameSuggestions, MaxNameSuggestions);

        ImGui.SetCursorPosX(x);
        if (_nameSuggestions.Count == 0)
        {
            Text(SecondaryColor, _newMember.Trim().Length == 0 ? "No other players online" : "No matching online player");
            return;
        }

        float right = x + width;
        var style = ImGui.GetStyle();
        for (int i = 0; i < _nameSuggestions.Count; i++)
        {
            string name = _nameSuggestions[i];
            float buttonWidth = ImGui.CalcTextSize(name).X + style.FramePadding.X * 2f;
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetCursorPosX() + buttonWidth > right) break;
            }

            if (ImGui.SmallButton($"{name}##suggest{i}")) _newMember = name;
            UIHelper.ShowTooltip("Online player, click to fill in");
        }
    }

    private static void Text(Vector4 color, string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static void DrawDivider(Vector2 windowPos, float x, float top, float bottom)
    {
        ImGui.GetWindowDrawList().AddLine(windowPos + new Vector2(x, top), windowPos + new Vector2(x, bottom), Theme.Colors.BorderDarkUint);
    }

    private static void OnSlotClicked(ItemSlot slot)
    {
        if (slot.HasItem && Input.IsAction("storage_item_quick_transfer"))
        {
            slot.TryMoveItemToConnectedStorage(out _);
        }
    }

    private static void OnClosed()
    {
        if (_player != null)
        {
            _player.Panel.ConnectStorage(_player.Inventory, true);
            _player.Inventory.ConnectStorage(_player.Panel);
        }

        _block?.PaymentStorage.DisconnectStorage();
        _block = null;
    }
}
