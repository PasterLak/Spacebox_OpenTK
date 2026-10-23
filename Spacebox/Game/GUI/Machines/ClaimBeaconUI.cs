using Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Commands;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Protection;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ClaimBeaconUI
{
    private const string WindowId = "claimBeacon";

    private static readonly List<KeyValuePair<string, int>> _sortedPayments = new List<KeyValuePair<string, int>>();
    private static ClaimSettings _sortedFor;

    private static ClaimBeaconBlock _block;
    private static LocalAstronaut _player;
    private static bool _wasVisible;
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
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;

        block.PaymentStorage.ConnectStorage(player.Inventory);
        player.Inventory.ConnectStorage(block.PaymentStorage);
        player.Panel.ConnectStorage(block.PaymentStorage);

        if (player.GameMode != GameMode.Survival)
            UIManager.Open(WindowId, "inventory", "creative");
        else
            UIManager.Open(WindowId, "inventory");
    }

    public static bool IsOpen => UIManager.IsOpen(WindowId) && _block != null;
    public static ClaimBeaconBlock Block => _block;
    public static string BlockName => _blockName;
    public static ItemSlot PaymentSlot => _block?.PaymentStorage.GetSlot(0, 0);
    public static bool CanManage => _block != null && ClaimMembersNetwork.CanManage(_block, GameSession.PlayerName, EntityProtection.LocalCanEdit);

    public static void SetShowZone(bool show) => ShowZone = show;

    public static void Update()
    {
        bool isVisible = IsOpen;
        if (_wasVisible && !isVisible) OnClosed();
        _wasVisible = isVisible;
        if (isVisible && Input.IsKeyDown(Keys.Escape)) UIManager.CloseAll();
    }

    public static void AddMember(string name)
    {
        if (_block == null || string.IsNullOrWhiteSpace(name)) return;
        ClaimMembersNetwork.Request(_block, name, true);
    }

    public static void RemoveMember(string name)
    {
        if (_block != null) ClaimMembersNetwork.Request(_block, name, false);
    }

    public static IReadOnlyList<string> NameSuggestionsFor(string typed)
    {
        _onlinePlayers.Clear();
        CommandSuggestionSources.AddPlayerNames(_onlinePlayers);
        _excludedNames.Clear();
        _excludedNames.AddRange(_block.Members);
        _excludedNames.Add(_block.Owner);
        _excludedNames.Add(GameSession.PlayerName);
        NameSuggestions.Filter(typed ?? "", _onlinePlayers, _excludedNames, _nameSuggestions, MaxNameSuggestions);
        return _nameSuggestions;
    }

    public static void Reset()
    {
        _block = null;
        _player = null;
        _wasVisible = false;
    }

    public static List<KeyValuePair<string, int>> SortedPayments()
    {
        var settings = ClaimSettings.Current;
        if (settings == _sortedFor && settings.Payments.Count == _sortedPayments.Count) return _sortedPayments;

        _sortedFor = settings;
        _sortedPayments.Clear();
        foreach (var payment in settings.Payments) _sortedPayments.Add(payment);
        _sortedPayments.Sort((a, b) => a.Value.CompareTo(b.Value));
        return _sortedPayments;
    }

    public static void OnSlotClicked(ItemSlot slot)
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
