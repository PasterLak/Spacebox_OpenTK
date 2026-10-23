using Engine.CanvasUI;
using Spacebox.Game.GUI.Canvas.Windows;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Network;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class PlayerListPanel : PanelContainer
{
    private const float Width = 300f;
    private const float ListHeight = 300f;
    private const float RefreshInterval = 0.5f;

    private readonly VBoxContainer _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly ConfirmDialog _banDialog = new ConfirmDialog();
    private readonly List<Label> _pings = new List<Label>();
    private readonly Label _title;
    private string _structure = "";
    private float _timer;

    private readonly PauseUI _pause;

    public PlayerListPanel(PauseUI pause)
    {
        _pause = pause;
        Name = "PlayerList";
        Visible = false;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddThemeStyleBoxOverride("panel", new StyleBoxEmpty());
        var frame = MenuLayout.Window("Players", Width);
        _title = frame.Title;
        frame.Footer.Visible = false;
        AddChild(frame.Root);
        _rows.AddThemeConstantOverride("separation", 4f);
        frame.Body.AddChild(MenuLayout.ScrollList(_rows, ListHeight));
    }

    public void Sync(float deltaTime)
    {
        bool shown = _pause.IsMultiplayer;
        if (Visible != shown)
        {
            Visible = shown;
            _timer = 0f;
        }
        if (!shown) return;

        _timer -= deltaTime;
        if (_timer > 0f) return;
        _timer = RefreshInterval;
        Build();
    }

    private void Build()
    {
        Show(PlayerListUI.Players, PlayerListUI.LocalPeer, PlayerListUI.CanModerate);
    }

    public void Show(IReadOnlyList<PlayerListEntry> players, int localPeer, bool canModerate)
    {
        string title = $"Players ({players.Count})";
        if (_title.Text != title) _title.Text = title;

        string structure = Structure(players, localPeer, canModerate);
        if (structure != _structure)
        {
            _structure = structure;
            _rows.ClearChildren();
            _pings.Clear();
            for (int i = 0; i < players.Count; i++) AddRow(players[i], localPeer, canModerate);
        }

        for (int i = 0; i < players.Count && i < _pings.Count; i++) StatusText.Set(_pings[i], PlayerListUI.PingText(players[i]), PlayerListUI.PingKind(players[i]));
    }

    private static string Structure(IReadOnlyList<PlayerListEntry> players, int localPeer, bool canModerate)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(localPeer).Append(canModerate ? '+' : '-');
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            builder.Append('|').Append(player.Peer).Append(player.Name).Append(player.IsMuted).Append(player.IsHost).Append(player.IsAdmin);
        }
        return builder.ToString();
    }

    private void AddRow(PlayerListEntry player, int localPeer, bool canModerate)
    {
        var row = _rows.AddChild(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 4f);
        var name = row.AddChild(new Label
        {
            Text = PlayerListUI.Label(player, localPeer),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Overrun = TextOverrun.Ellipsis,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (player.IsMuted) StatusText.SetKind(name, StatusKind.Muted);
        var ping = row.AddChild(StatusText.Create(PlayerListUI.PingText(player), PlayerListUI.PingKind(player)));
        ping.VerticalAlignment = VerticalAlignment.Center;
        _pings.Add(ping);

        if (!canModerate || !PlayerListUI.CanModeratePlayer(player, localPeer)) return;
        AddAction(row, "Kick", () => PlayerListUI.Kick(player));
        AddAction(row, player.IsMuted ? "Unmute" : "Mute", () => PlayerListUI.ToggleMute(player));
        AddAction(row, "Ban", () => ConfirmBan(player));
    }

    private static void AddAction(HBoxContainer row, string text, Action action)
    {
        var button = row.AddChild(new Button { Text = text, ThemeTypeVariation = "SmallButton", CustomMinimumSize = new OpenTK.Mathematics.Vector2(0f, 20f) });
        button.Pressed += _ => action();
    }

    private void ConfirmBan(PlayerListEntry player)
    {
        if (Root == null) return;
        int peer = player.Peer;
        _banDialog.Show(Root, "Ban player?", $"Ban {player.Name}? They will not be able to join again.", new[] { "Ban", "Cancel" }, result =>
        {
            if (result == 0) PlayerListUI.Ban(peer);
        });
    }
}
