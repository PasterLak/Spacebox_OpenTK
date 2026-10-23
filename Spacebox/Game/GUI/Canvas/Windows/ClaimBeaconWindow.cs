using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.Commands;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Canvas.Items;
using Spacebox.Game.Protection;

namespace Spacebox.Game.GUI.Canvas.Windows;

public sealed class ClaimBeaconWindow : GameWindow
{
    private const float ColumnHeight = 170f;
    private const float PaymentIconSize = 20f;
    private static readonly Color4 BarFill = new Color4(0f, 0.8f, 0.35f, 1f);
    private static readonly Color4 BarLow = new Color4(1f, 0.6f, 0f, 1f);

    private readonly Label _owner = new Label();
    private readonly Label _protection = StatusText.Create();
    private readonly ProgressBar _time = new ProgressBar { MaxValue = 1f, ShowPercentage = false, CustomMinimumSize = new Vector2(160f, 8f) };
    private readonly Label _area = StatusText.Create("", StatusKind.Muted);
    private readonly HBoxContainer _payRow = new HBoxContainer();
    private readonly ItemSlotControl _slot = new ItemSlotControl();
    private readonly VBoxContainer _payments = new VBoxContainer();
    private readonly Label _membersTitle = new Label();
    private readonly VBoxContainer _members = new VBoxContainer();
    private readonly HBoxContainer _addRow = new HBoxContainer();
    private readonly LineEdit _newMember = new LineEdit { PlaceholderText = "Player name", MaxLength = ClaimSettings.MaxNameLength, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly HFlowContainer _suggestions = new HFlowContainer();
    private readonly Label _noSuggestions = StatusText.Create("", StatusKind.Muted);
    private readonly CheckBox _showZone = new CheckBox("Show area", true);
    private ClaimBeaconBlock _shown;
    private ClaimSettings _paymentsFor;
    private int _membersVersion = -1;
    private string _suggestionsFor;

    private readonly ClaimBeaconUI _claimBeacon;

    public ClaimBeaconWindow(ClaimBeaconUI claimBeacon) : base("Claim Beacon")
    {
        _claimBeacon = claimBeacon;
        Name = "ClaimBeacon";
        var row = Body.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 10f);

        var status = row.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(170f, ColumnHeight) });
        status.AddChild(_owner);
        status.AddChild(_protection);
        status.AddChild(_time);
        status.AddChild(_area);
        status.AddChild(_payRow);
        _payRow.AddChild(_slot);
        _payRow.AddChild(new Label { Text = "Pay with ingots", VerticalAlignment = VerticalAlignment.Center, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        _slot.Clicked += _claimBeacon.OnSlotClicked;

        row.AddChild(new VSeparator());
        var payments = row.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(110f, ColumnHeight) });
        payments.AddChild(new Label { Text = "Payment", ThemeTypeVariation = "SectionLabel" });
        var paymentScroll = payments.AddChild(new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill });
        paymentScroll.AddChild(_payments);

        row.AddChild(new VSeparator());
        var members = row.AddChild(new VBoxContainer { CustomMinimumSize = new Vector2(190f, ColumnHeight) });
        members.AddChild(_membersTitle);
        var memberScroll = members.AddChild(new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill });
        memberScroll.AddChild(_members);
        _members.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        members.AddChild(_addRow);
        _addRow.AddChild(_newMember);
        var add = _addRow.AddChild(new Button { Text = "Add", ThemeTypeVariation = "SmallButton" });
        add.Pressed += _ => SubmitMember();
        _newMember.TextSubmitted += _ => SubmitMember();
        members.AddChild(_suggestions);
        members.AddChild(_noSuggestions);
        members.AddChild(_showZone);
        _showZone.Toggled += (_, pressed) => _claimBeacon.SetShowZone(pressed);
    }

    public override bool IsOpen => _claimBeacon.IsOpen;

    protected override void Refresh()
    {
        var block = _claimBeacon.Block;
        var settings = ClaimSettings.Current;
        if (TitleLabel.Text != _claimBeacon.BlockName) TitleLabel.Text = _claimBeacon.BlockName;
        if (!ReferenceEquals(block, _shown))
        {
            _shown = block;
            _slot.Bind(_claimBeacon.PaymentSlot);
            _membersVersion = -1;
            _newMember.Text = "";
        }

        _slot.Refresh();
        SetText(_owner, $"Owner: {(block.Owner.Length > 0 ? block.Owner : "nobody")}");
        bool enabled = settings.Enabled;
        if (!enabled) StatusText.Set(_protection, "Claims are disabled", StatusKind.Bad);
        else if (block.IsClaimActive) StatusText.Set(_protection, $"Protected: {ClaimCost.FormatDuration(block.RemainingSeconds)}", StatusKind.Good);
        else StatusText.Set(_protection, "Not protected", StatusKind.Bad);

        _time.Visible = enabled;
        _area.Visible = enabled;
        _payRow.Visible = enabled;
        _time.Value = settings.MaxStoredSeconds > 0 ? Math.Clamp(block.RemainingSeconds / settings.MaxStoredSeconds, 0f, 1f) : 0f;
        _time.FillColor = block.RemainingSeconds <= settings.WarningSeconds ? BarLow : BarFill;
        _time.TooltipTitle = $"Stores up to {ClaimCost.FormatDuration(settings.MaxStoredSeconds)}";
        _time.TooltipText = settings.ExtraClaimCostPercent > 0 ? $"Each extra claim you own uses time {settings.ExtraClaimCostPercent}% faster" : "";
        int size = settings.Radius * 2 + 1;
        StatusText.Set(_area, block.CoversWholeEntity ? "Area: whole ship" : $"Area: {size}x{size}x{size}", StatusKind.Muted);

        if (!ReferenceEquals(settings, _paymentsFor)) BuildPayments(settings);
        RefreshMembers(block, settings);
        if (_showZone.ButtonPressed != _claimBeacon.ShowZone) _showZone.ButtonPressed = _claimBeacon.ShowZone;
    }

    private void BuildPayments(ClaimSettings settings)
    {
        _paymentsFor = settings;
        _payments.ClearChildren();
        foreach (var payment in _claimBeacon.SortedPayments())
        {
            var row = _payments.AddChild(new HBoxContainer());
            row.AddThemeConstantOverride("separation", 4f);
            var icon = row.AddChild(new ItemIconControl(PaymentIconSize));
            if (ItemLookup.TryFind(payment.Key, out var item)) icon.Set(item.Icon, 0, item.Name);
            else icon.Set(default, 0, payment.Key);
            row.AddChild(new Label { Text = $"+{ClaimCost.FormatDuration(payment.Value * 60f)}", VerticalAlignment = VerticalAlignment.Center });
        }
    }

    private void RefreshMembers(ClaimBeaconBlock block, ClaimSettings settings)
    {
        bool canManage = _claimBeacon.CanManage;
        SetText(_membersTitle, $"Members {block.Members.Count}/{settings.MaxMembers}");
        _addRow.Visible = canManage;
        _suggestions.Visible = canManage;

        int version = block.Members.Count * 31 + (canManage ? 1 : 0) + string.Join(",", block.Members).GetHashCode();
        if (version != _membersVersion)
        {
            _membersVersion = version;
            _members.ClearChildren();
            if (block.Members.Count == 0) _members.AddChild(StatusText.Create("No members yet", StatusKind.Muted));
            foreach (var member in block.Members)
            {
                var row = _members.AddChild(new HBoxContainer());
                row.AddChild(new Label { Text = member, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center });
                if (!canManage) continue;
                string name = member;
                var remove = row.AddChild(new Button { Text = "x", ThemeTypeVariation = "SmallButton", TooltipText = "Remove member" });
                remove.Pressed += _ => _claimBeacon.RemoveMember(name);
            }
            _suggestionsFor = null;
        }

        if (canManage) RefreshSuggestions();
        else _noSuggestions.Visible = false;
    }

    private void RefreshSuggestions()
    {
        string typed = _newMember.Text;
        var names = _claimBeacon.NameSuggestionsFor(typed);
        string key = typed + "|" + string.Join(",", names);
        if (key == _suggestionsFor) return;
        _suggestionsFor = key;

        _suggestions.ClearChildren();
        foreach (var name in names)
        {
            string value = name;
            var button = _suggestions.AddChild(new Button { Text = name, ThemeTypeVariation = "SmallButton", TooltipText = "Online player, click to fill in" });
            button.Pressed += _ => _newMember.Text = value;
        }
        _noSuggestions.Visible = names.Count == 0;
        StatusText.Set(_noSuggestions, typed.Trim().Length == 0 ? "No other players online" : "No matching online player", StatusKind.Muted);
    }

    private void SubmitMember()
    {
        string name = _newMember.Text.Trim();
        if (name.Length == 0) return;
        _claimBeacon.AddMember(name);
        _newMember.Text = "";
    }

    private static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
}
