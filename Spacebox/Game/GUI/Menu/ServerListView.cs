using System.Numerics;
using ImGuiNET;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public sealed class ServerListView
{
    private static readonly Vector4 SectionColor = new Vector4(1f, 0.8f, 0.2f, 1f);

    private readonly ServerIconCache _icons;

    public ServerListItem Selected { get; set; }

    public event Action<ServerListItem> Activated;

    public ServerListView(ServerIconCache icons)
    {
        _icons = icons;
    }

    public void Draw(ServerBrowser browser, Vector2 size)
    {
        if (ImGui.BeginChild("##servers", size, ImGuiChildFlags.Border))
        {
            float width = ImGui.GetContentRegionAvail().X;
            float rowHeight = ImGui.GetTextLineHeight() * 3.6f;

            DrawSection($"Saved servers ({browser.Saved.Count})");
            if (browser.Saved.Count == 0) DrawHint("No saved servers yet. Press \"Add server\" to add one.");
            for (int i = 0; i < browser.Saved.Count; i++)
                DrawRow(browser.Saved[i], width, rowHeight);

            ImGui.Dummy(new Vector2(0f, rowHeight * 0.2f));
            DrawSection("Local network");
            int shown = 0;
            for (int i = 0; i < browser.Local.Count; i++)
            {
                var item = browser.Local[i];
                if (browser.IsHiddenLocal(item)) continue;
                DrawRow(item, width, rowHeight);
                shown++;
            }
            if (shown == 0) DrawHint(browser.Local.Count == 0 ? "Searching for servers in your network..." : "No other servers found in your network.");
        }
        ImGui.EndChild();
    }

    private static void DrawSection(string title)
    {
        ImGui.Dummy(new Vector2(0f, 2f));
        ImGui.TextColored(SectionColor, " " + title);
        ImGui.Dummy(new Vector2(0f, 2f));
    }

    private static void DrawHint(string text)
    {
        ImGui.TextDisabled("   " + text);
    }

    private void DrawRow(ServerListItem item, float width, float rowHeight)
    {
        ImGui.PushID(item.Key);
        var start = ImGui.GetCursorScreenPos();

        if (ImGui.Selectable("##row", item == Selected, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(width, rowHeight)))
        {
            Selected = item;
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) Activated?.Invoke(item);
        }

        var draw = ImGui.GetWindowDrawList();
        float pad = rowHeight * 0.1f;
        float lineHeight = ImGui.GetTextLineHeight();
        uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
        uint mutedColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);

        var iconMin = start + new Vector2(pad, pad);
        var iconMax = iconMin + new Vector2(rowHeight - pad * 2f);
        draw.AddRectFilled(iconMin - Vector2.One, iconMax + Vector2.One, Theme.Colors.BorderDarkUint);
        draw.AddImage(_icons.Get(item.IconHash).Handle, iconMin, iconMax);

        float rightWidth = rowHeight * 2.4f;
        float textX = iconMax.X + pad * 2f;
        float textWidth = start.X + width - rightWidth - textX - pad;
        float lineGap = (rowHeight - pad * 2f - lineHeight * 3f) * 0.5f;
        float y = start.Y + pad;

        draw.AddText(new Vector2(textX, y), textColor, ServerText.Fit(item.DisplayName, textWidth));
        draw.AddText(new Vector2(textX, y + lineHeight + lineGap), mutedColor, ServerText.Fit(ServerText.FirstLine(item.Description), textWidth));
        draw.AddText(new Vector2(textX, y + (lineHeight + lineGap) * 2f), mutedColor, ServerText.Fit(ServerText.AddressLine(item), textWidth));

        DrawStatus(draw, item, new Vector2(start.X + width - pad, y), lineHeight + lineGap, rightWidth);
        ImGui.PopID();
    }

    private static void DrawStatus(ImDrawListPtr draw, ServerListItem item, Vector2 topRight, float lineStep, float width)
    {
        string state = ServerText.StateLabel(item);
        var stateSize = ImGui.CalcTextSize(state);
        float barHeight = ImGui.GetTextLineHeight() * 0.85f;
        float barsWidth = ServerSignalBars.Width(barHeight);
        float x = topRight.X - stateSize.X - barsWidth - 6f;

        ServerSignalBars.Draw(draw, new Vector2(topRight.X - barsWidth, topRight.Y), barHeight, item);
        draw.AddText(new Vector2(x, topRight.Y), ImGui.GetColorU32(ServerText.StateColor(item)), state);

        string players = ServerText.PlayersLabel(item);
        var playersSize = ImGui.CalcTextSize(players);
        draw.AddText(new Vector2(topRight.X - playersSize.X, topRight.Y + lineStep), ImGui.GetColorU32(ImGuiCol.Text), players);

        if (item.Status == null) return;

        string version = ServerText.Fit(ServerText.VersionLabel(item), width);
        var versionSize = ImGui.CalcTextSize(version);
        var versionColor = item.IsCompatible ? ImGui.GetColorU32(ImGuiCol.TextDisabled) : ImGui.GetColorU32(ServerText.ErrorColor);
        draw.AddText(new Vector2(topRight.X - versionSize.X, topRight.Y + lineStep * 2f), versionColor, version);
    }
}
