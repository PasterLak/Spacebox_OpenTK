using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
    private static readonly Vector4 ActiveColor = new Vector4(0f, 1f, 0.4f, 1f);
    private static readonly Vector4 InactiveColor = new Vector4(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 HintColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);

    private static readonly Action<ItemSlot> _onSlotClicked = OnSlotClicked;

    private static ClaimBeaconBlock _block;
    private static LocalAstronaut _player;
    private static bool _wasVisible;
    private static string _newMember = "";

    public static bool ShowZone { get; private set; } = true;
    public static ClaimBeaconBlock OpenBlock => UIManager.IsOpen(WindowId) ? _block : null;

    public static void Open(ClaimBeaconBlock block, LocalAstronaut player)
    {
        _block = block;
        _player = player;
        _newMember = "";

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
        float windowWidth = displaySize.Y * 0.5f;
        float windowHeight = displaySize.Y * 0.5f;
        float padding = windowHeight / 25f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("ClaimBeacon", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration);
        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, "Claim Beacon");
        ImGui.SetCursorPosX(padding);
        ImGui.Text($"Owner: {(_block.Owner.Length > 0 ? _block.Owner : "nobody")}");

        DrawStatus(padding);
        DrawPayment(padding);
        DrawMembers(padding);

        ImGui.SetCursorPosX(padding);
        bool showZone = ShowZone;
        if (ImGui.Checkbox("Show protected area", ref showZone)) ShowZone = showZone;

        ImGui.End();
    }

    public static void Reset()
    {
        _block = null;
        _player = null;
        _wasVisible = false;
        _newMember = "";
    }

    private static void DrawStatus(float padding)
    {
        var settings = ClaimSettings.Current;
        ImGui.SetCursorPosX(padding);
        if (!settings.Enabled)
        {
            ImGui.TextColored(InactiveColor, "Claims are disabled on this server");
            return;
        }

        if (_block.IsClaimActive)
        {
            ImGui.TextColored(ActiveColor, $"Protected: {ClaimCost.FormatDuration(_block.RemainingSeconds)} left");
        }
        else
        {
            ImGui.TextColored(InactiveColor, "Not protected: pay with ingots");
        }

        ImGui.SetCursorPosX(padding);
        string area = _block.CoversWholeEntity ? "the whole ship" : $"{settings.Radius * 2 + 1}x{settings.Radius * 2 + 1}x{settings.Radius * 2 + 1} blocks";
        ImGui.TextColored(HintColor, $"Protects {area}. More claims use time faster (+{settings.ExtraClaimCostPercent}% each).");
    }

    private static void DrawPayment(float padding)
    {
        var settings = ClaimSettings.Current;
        ImGui.SetCursorPosX(padding);
        var slot = _block.PaymentStorage.GetSlot(0, 0);
        InventoryUIHelper.DrawSlot(slot, "claimBeaconPayment", _onSlotClicked);
        InventoryUIHelper.ShowTooltip(slot, false, true);

        foreach (var payment in settings.Payments)
        {
            ImGui.SetCursorPosX(padding);
            ImGui.TextColored(HintColor, $"{payment.Key}: +{ClaimCost.FormatDuration(payment.Value * 60f)}");
        }

        ImGui.SetCursorPosX(padding);
        ImGui.TextColored(HintColor, $"Stores up to {ClaimCost.FormatDuration(settings.MaxStoredSeconds)}");
    }

    private static void DrawMembers(float padding)
    {
        bool canManage = ClaimMembersNetwork.CanManage(_block, GameSession.PlayerName, EntityProtection.LocalCanEdit);

        ImGui.SetCursorPosX(padding);
        ImGui.Text($"Members ({_block.Members.Count}/{ClaimSettings.Current.MaxMembers}):");

        for (int i = 0; i < _block.Members.Count; i++)
        {
            string member = _block.Members[i];
            ImGui.SetCursorPosX(padding * 2f);
            ImGui.Text(member);
            if (!canManage) continue;

            ImGui.SameLine();
            if (ImGui.SmallButton($"Remove##{i}")) ClaimMembersNetwork.Request(_block, member, false);
        }

        if (!canManage) return;

        ImGui.SetCursorPosX(padding);
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() * 0.5f);
        bool submit = ImGui.InputText("##newMember", ref _newMember, (uint)ClaimSettings.MaxNameLength, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.SameLine();
        if ((ImGui.Button("Add member") || submit) && _newMember.Trim().Length > 0)
        {
            ClaimMembersNetwork.Request(_block, _newMember, true);
            _newMember = "";
        }
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
