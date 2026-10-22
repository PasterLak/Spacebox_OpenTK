using System.Numerics;
using ImGuiNET;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public sealed class ServerInfoWindow
{
    private static readonly Vector4 TitleColor = new Vector4(1f, 0.8f, 0.2f, 1f);

    private readonly GameMenu _menu;
    private readonly ServerIconCache _icons;
    private readonly Action<ServerListItem, string, string, int> _onSave;
    private readonly Action<ServerListItem> _onJoin;
    private readonly Action<ServerListItem> _onRefresh;

    private ServerListItem _item;
    private bool _addMode;
    private string _name = "";
    private string _host = "";
    private int _port;
    private string _error;

    public bool IsOpen { get; private set; }

    public ServerInfoWindow(GameMenu menu, ServerIconCache icons,
        Action<ServerListItem, string, string, int> onSave,
        Action<ServerListItem> onJoin,
        Action<ServerListItem> onRefresh)
    {
        _menu = menu;
        _icons = icons;
        _onSave = onSave;
        _onJoin = onJoin;
        _onRefresh = onRefresh;
    }

    public void OpenAdd()
    {
        _item = null;
        _addMode = true;
        _name = "";
        _host = "";
        _port = Engine.Networking.NetworkManager.DefaultPort;
        _error = null;
        IsOpen = true;
    }

    public void OpenInfo(ServerListItem item)
    {
        if (item == null) return;

        _item = item;
        _addMode = false;
        _name = item.DisplayName;
        _host = item.Entry.IP;
        _port = item.Entry.Port;
        _error = null;
        IsOpen = true;
        _onRefresh?.Invoke(item);
    }

    public void Close()
    {
        IsOpen = false;
        _item = null;
    }

    public void Render()
    {
        if (!IsOpen) return;

        var display = ImGui.GetIO().DisplaySize;
        float padding = MathF.Max(12f, display.Y * 0.025f);
        float buttonHeight = MathF.Max(28f, display.Y * 0.045f);
        float width = MathF.Min(MathF.Max(display.X * 0.42f, 480f), display.X * 0.95f);
        float height = _addMode
            ? padding * 3.5f + ImGui.GetTextLineHeightWithSpacing() + FieldsHeight() + buttonHeight
            : MathF.Max(display.Y * 0.7f, 460f);
        height = MathF.Min(height, display.Y * 0.95f);
        var position = GameMenu.CenterNextWindow(width, height);

        ImGui.SetNextWindowFocus();
        ImGui.Begin("##serverInfo", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        GameMenu.DrawElementColors(position, new Vector2(width, height), display.Y);

        float contentWidth = width - padding * 2f;

        ImGui.SetCursorPos(new Vector2(padding, padding));
        if (_addMode) ImGui.TextColored(TitleColor, "Add server");
        else DrawHeader(contentWidth);

        float fieldsTop = ImGui.GetCursorPosY() + padding * 0.5f;
        float fieldsHeight = FieldsHeight();
        float buttonsY = height - padding - buttonHeight;
        float fieldsY = _addMode ? fieldsTop : buttonsY - padding - fieldsHeight;

        if (!_addMode) DrawDetails(padding, contentWidth, fieldsY - padding * 0.5f);

        ImGui.SetCursorPos(new Vector2(padding, fieldsY));
        DrawFields(contentWidth);
        DrawButtons(padding, contentWidth, buttonsY, buttonHeight);

        ImGui.End();

        if (ImGui.IsKeyPressed(ImGuiKey.Escape)) Close();
    }

    private void DrawHeader(float contentWidth)
    {
        float iconSize = ImGui.GetTextLineHeight() * 5f;
        var start = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        draw.AddRectFilled(start - Vector2.One * 2f, start + new Vector2(iconSize + 2f), Theme.Colors.BorderDarkUint);
        draw.AddImage(_icons.Get(_item.IconHash).Handle, start, start + new Vector2(iconSize));

        float textX = ImGui.GetCursorPosX() + iconSize + 14f;
        float textWidth = contentWidth - iconSize - 14f;
        float y = ImGui.GetCursorPosY();
        float line = ImGui.GetTextLineHeightWithSpacing() + 3f;

        ImGui.SetCursorPos(new Vector2(textX, y));
        ImGui.TextColored(TitleColor, ServerText.Fit(_item.Status?.Name ?? _item.DisplayName, textWidth));
        ImGui.SetCursorPos(new Vector2(textX, y + line));
        ImGui.TextDisabled(ServerText.Fit(ServerText.AddressLine(_item), textWidth));
        ImGui.SetCursorPos(new Vector2(textX, y + line * 2f));
        ImGui.TextColored(ServerText.StateColor(_item), _item.State == ServerOnlineState.Online ? $"Online, {_item.PingMs} ms" : ServerText.StateLabel(_item));
        ImGui.SetCursorPos(new Vector2(textX, y + line * 3f));
        ImGui.Text(ServerText.PlayersLabel(_item));

        ImGui.SetCursorPosY(y + iconSize + 8f);
    }

    private void DrawDetails(float padding, float contentWidth, float bottom)
    {
        var status = _item.Status;
        float labelWidth = ImGui.CalcTextSize("Game mode:  ").X;
        float line = ImGui.GetTextLineHeightWithSpacing() + 2f;

        if (status != null)
        {
            DrawDetail(padding, labelWidth, "Version:", ServerText.VersionLabel(_item), _item.IsCompatible ? null : ServerText.ErrorColor);
            DrawDetail(padding, labelWidth, "GameSet:", GameSetLabel(status.ModId), null);
            DrawDetail(padding, labelWidth, "Game mode:", ServerText.GameModeLabel(status.GameMode), null);
            DrawDetail(padding, labelWidth, "World:", status.WorldName, null);
            DrawDetail(padding, labelWidth, "Type:", status.IsDedicated ? "Dedicated server" : "Hosted by a player", null);
        }

        float top = ImGui.GetCursorPosY() + line * 0.3f;
        float boxHeight = bottom - top;
        if (boxHeight < line * 2f) return;

        ImGui.SetCursorPos(new Vector2(padding, top));
        if (ImGui.BeginChild("##description", new Vector2(contentWidth, boxHeight), ImGuiChildFlags.Border))
        {
            string description = _item.Description;
            if (string.IsNullOrWhiteSpace(description)) ImGui.TextDisabled(status == null ? "No information - the server did not answer." : "No description.");
            else ImGui.TextWrapped(description);
        }
        ImGui.EndChild();
    }

    private static void DrawDetail(float x, float labelWidth, string label, string value, Vector4? color)
    {
        float y = ImGui.GetCursorPosY();
        ImGui.SetCursorPos(new Vector2(x, y));
        ImGui.TextDisabled(label);
        ImGui.SetCursorPos(new Vector2(x + labelWidth, y));
        if (color.HasValue) ImGui.TextColored(color.Value, value ?? "");
        else ImGui.Text(value ?? "");
    }

    private string GameSetLabel(string modId)
    {
        var mod = _menu.GetModById(modId);
        return mod != null ? $"{mod.ModName} ({modId})" : $"{modId} (downloaded on join)";
    }

    private static float FieldsHeight()
    {
        float row = ImGui.GetFrameHeight() + ImGui.GetTextLineHeight() * 0.6f;
        return row * 3f + ImGui.GetTextLineHeightWithSpacing();
    }

    private void DrawFields(float contentWidth)
    {
        float x = ImGui.GetCursorPosX();
        float labelWidth = ImGui.CalcTextSize("Address  ").X;
        float inputWidth = contentWidth - labelWidth;
        float row = ImGui.GetFrameHeight() + ImGui.GetTextLineHeight() * 0.6f;
        float y = ImGui.GetCursorPosY();

        DrawField(x, y, labelWidth, inputWidth, "Name", DrawNameField);
        DrawField(x, y + row, labelWidth, inputWidth, "Address", DrawHostField);
        DrawField(x, y + row * 2f, labelWidth, inputWidth * 0.35f, "Port", () => ImGui.InputInt("##port", ref _port, 0));

        ImGui.SetCursorPos(new Vector2(x, y + row * 3f));
        if (!string.IsNullOrEmpty(_error)) ImGui.TextColored(ServerText.ErrorColor, _error);
        else if (_item != null && !_item.IsSaved) ImGui.TextDisabled("Found in your local network.");
    }

    private bool DrawNameField()
    {
        bool changed = ImGui.InputText("##name", ref _name, ServerAddressValidator.MaxNameLength);
        return ClipboardMenu.ForTextField("##nameMenu", ref _name, ServerAddressValidator.MaxNameLength) || changed;
    }

    private bool DrawHostField()
    {
        bool changed = ImGui.InputText("##host", ref _host, ServerAddressValidator.MaxHostLength);
        if (!ClipboardMenu.ForTextField("##hostMenu", ref _host, ServerAddressValidator.MaxHostLength)) return changed;

        SplitPort();
        return true;
    }

    private void SplitPort()
    {
        ServerAddressValidator.Resolve(_host, _port, out _host, out _port);
    }

    private static void DrawField(float x, float y, float labelWidth, float inputWidth, string label, Func<bool> input)
    {
        float frame = ImGui.GetFrameHeight();
        ImGui.SetCursorPos(new Vector2(x, y + (frame - ImGui.GetTextLineHeight()) * 0.5f));
        ImGui.Text(label);
        ImGui.SetCursorPos(new Vector2(x + labelWidth, y));
        ImGui.SetNextItemWidth(inputWidth);
        input();
    }

    private void DrawButtons(float padding, float contentWidth, float y, float height)
    {
        float spacing = padding * 0.6f;
        int count = _addMode ? 2 : 3;
        float width = (contentWidth - spacing * (count - 1)) / count;

        string saveLabel = _addMode ? "Add" : _item.IsSaved ? "Save" : "Add to list";
        _menu.ButtonWithBackground(saveLabel, new Vector2(width, height), new Vector2(padding, y), Save);

        if (!_addMode)
        {
            _menu.ButtonWithBackground("Join", new Vector2(width, height), new Vector2(padding + width + spacing, y), () =>
            {
                var item = _item;
                Close();
                _onJoin?.Invoke(item);
            });
        }

        _menu.ButtonWithBackground(_addMode ? "Cancel" : "Close", new Vector2(width, height),
            new Vector2(padding + (width + spacing) * (count - 1), y), Close);
    }

    private void Save()
    {
        SplitPort();
        if (!ServerAddressValidator.TryValidate(_host, _port, out _error)) return;

        _onSave?.Invoke(_item, ServerAddressValidator.ResolveName(_name, _host), _host.Trim(), _port);
        Close();
    }
}
