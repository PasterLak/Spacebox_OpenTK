using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class ShipSharingNetwork
{
    public const float MaxPlaceDistance = 256f;

    private static readonly PeerRateLimiter Requests = new PeerRateLimiter(2, 0.2);

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ShipPlaceRequestMessage>(OnPlaceRequest);
    }

    public static void RequestPlace(CompoundTag shipEntity, Vector3 center, Quaternion rotation, Vector3 placerPosition, Action<string, bool> report)
    {
        var network = NetworkManager.Instance;
        if (network.IsRemoteClient)
        {
            network.SendToServer(new ShipPlaceRequestMessage { Data = ShipFileCodec.ToBytes(shipEntity), Center = center, Rotation = rotation });
            report?.Invoke("Placing the ship...", true);
            return;
        }

        bool placed = Place(shipEntity, center, rotation, GameSession.PlayerName, ProtectionNetwork.CanPeerEdit(network.LocalPeerId), placerPosition, out string message);
        report?.Invoke(message, placed);
    }

    public static string CheckLocal(ShipPlacementBounds bounds, Vector3 placerPosition)
    {
        var network = NetworkManager.Instance;
        return ShipPlacementCheck.Find(bounds, GameSession.PlayerName, ProtectionNetwork.CanPeerEdit(network.LocalPeerId), placerPosition);
    }

    private static bool Place(CompoundTag shipEntity, Vector3 center, Quaternion rotation, string owner, bool isAdmin, Vector3? placerPosition, out string message)
    {
        if (!ShipPlacer.TryPlace(shipEntity, center, rotation, owner, isAdmin, placerPosition, out message, out var entity)) return false;

        Debug.Log($"[Ships] {owner} placed ship {entity.EntityID} at {entity.PositionWorld}");
        if (NetworkManager.Instance.IsServer && NetworkManager.Instance.IsActive) SectorNetwork.BroadcastManifest(entity.Sector);
        return true;
    }

    private static void OnPlaceRequest(int sender, ShipPlaceRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || sender == network.LocalPeerId) return;

        string name = PlayerRegistry.GetName(sender);
        string error = Check(sender, name, message);
        if (error == null)
        {
            try
            {
                var entity = ShipFileCodec.FromBytes(message.Data);
                var placer = Combat.PlayerHitboxes.FindPlayer(sender);
                bool placed = Place(entity, message.Center, message.Rotation, name, ProtectionNetwork.CanPeerEdit(sender), placer?.Position, out error);
                ChatService.SendToPlayer(name, error, placed ? Color4.LightGreen : Color4.OrangeRed);
                return;
            }
            catch (Exception ex)
            {
                error = "The ship data is damaged: " + ex.Message;
            }
        }

        ChatService.SendToPlayer(name, error, Color4.OrangeRed);
    }

    private static string Check(int sender, string name, ShipPlaceRequestMessage message)
    {
        bool allowed = Players.PlayerGameModeService.RulesOf(name).ShipLibrary;
        if (!allowed && !Admin.ServerAdmins.IsAdmin(name)) return "Your game mode cannot place saved ships (admins can)";
        if (!Requests.TryConsume(sender, Time.Total)) return "Wait a moment before placing another ship";
        if (message.Data == null || message.Data.Length == 0) return "The ship data is missing";
        if (!NetValidation.IsFinite(message.Center)) return "Invalid position";

        var player = Combat.PlayerHitboxes.FindPlayer(sender);
        if (player == null || Vector3.DistanceSquared(player.Position, message.Center) > MaxPlaceDistance * MaxPlaceDistance) return "That place is too far away";
        return null;
    }

    public static void ForgetPeer(int peer) => Requests.Forget(peer);
}
