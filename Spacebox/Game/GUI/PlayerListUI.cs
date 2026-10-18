using System.Numerics;
using Engine.Networking;
using ImGuiNET;
using Spacebox.Game.GUI.Menu;
using Spacebox.Network;
using Spacebox.Network.Moderation;

namespace Spacebox.Game.GUI;

public static class PlayerListUI
{
    private const string BanPopupId = "Ban player?";

    private static readonly Vector4 GoodPingColor = new Vector4(0.4f, 1f, 0.4f, 1f);
    private static readonly Vector4 MediumPingColor = new Vector4(1f, 0.85f, 0.2f, 1f);
    private static readonly Vector4 BadPingColor = new Vector4(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 MutedColor = new Vector4(0.6f, 0.6f, 0.6f, 1f);

    private static int _banPeer = -1;
    private static string _banName = string.Empty;

    public static void Draw()
    {
        var network = NetworkManager.Instance;
        var players = PlayerListNetwork.Players;
        var displaySize = ImGui.GetIO().DisplaySize;
        var size = new Vector2(displaySize.X * 0.22f, displaySize.Y * 0.5f);
        var position = new Vector2(displaySize.X - size.X - displaySize.X * 0.03f, (displaySize.Y - size.Y) * 0.5f);

        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(size);
        ImGui.Begin("PlayerList", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar);
        GameMenu.DrawElementColors(position, size, displaySize.Y, 0.005f);

        ImGui.Text($"Players ({players.Count})");
        ImGui.Separator();

        bool canModerate = network.IsServer && GameSession.HasLocalPlayer;
        int columns = canModerate ? 3 : 2;

        if (ImGui.BeginTable("PlayerListTable", columns, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Ping", ImGuiTableColumnFlags.WidthFixed);
            if (canModerate) ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed);

            for (int i = 0; i < players.Count; i++)
                DrawRow(players[i], network.LocalPeerId, canModerate);

            ImGui.EndTable();
        }

        DrawBanPopup();
        ImGui.End();
    }

    private static void DrawRow(PlayerListEntry player, int localPeer, bool canModerate)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        string label = player.Name;
        if (player.IsHost) label += " (host)";
        if (player.Peer == localPeer) label += " (you)";
        if (player.IsMuted) label += " [muted]";

        if (player.IsMuted) ImGui.TextColored(MutedColor, label);
        else ImGui.Text(label);

        ImGui.TableNextColumn();
        if (player.IsHost) ImGui.Text("-");
        else ImGui.TextColored(GetPingColor(player.Ping), $"{player.Ping} ms");

        if (!canModerate) return;

        ImGui.TableNextColumn();
        if (player.IsHost || player.Peer == localPeer) return;

        ImGui.PushID(player.Peer);

        if (ImGui.SmallButton("Kick")) ServerModeration.Kick(player.Peer);

        ImGui.SameLine();
        if (ImGui.SmallButton(player.IsMuted ? "Unmute" : "Mute")) ServerModeration.SetMuted(player.Name, !player.IsMuted);

        ImGui.SameLine();
        if (ImGui.SmallButton("Ban"))
        {
            _banPeer = player.Peer;
            _banName = player.Name;
            ImGui.OpenPopup(BanPopupId);
        }

        ImGui.PopID();
    }

    private static void DrawBanPopup()
    {
        if (!ImGui.BeginPopupModal(BanPopupId, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove)) return;

        ImGui.Text($"Ban {_banName}? They will not be able to join again.");

        if (ImGui.Button("Ban", new Vector2(120f, 0f)))
        {
            ServerModeration.Ban(_banPeer);
            _banPeer = -1;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120f, 0f)))
        {
            _banPeer = -1;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static Vector4 GetPingColor(int ping)
    {
        if (ping < 80) return GoodPingColor;
        return ping < 200 ? MediumPingColor : BadPingColor;
    }
}
