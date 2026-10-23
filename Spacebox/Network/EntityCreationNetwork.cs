using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class EntityCreationNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<EntityCreateRequestMessage>(OnCreateRequest);
        network.Messages.Register<EntityCreatedMessage>(OnCreated);
    }

    public static void PlaceNewEntity(Sector sector, Vector3 positionWorld, Block block)
    {
        if (sector == null || block == null) return;

        var network = NetworkManager.Instance;
        if (network.IsRemoteClient)
        {
            network.SendToServer(new EntityCreateRequestMessage
            {
                PositionWorld = positionWorld,
                BlockId = block.Id,
                Direction = (byte)block.Direction,
                Rotation = (byte)block.Rotation
            });
            return;
        }

        string error = Spacebox.Game.Protection.ClaimPlacement.CheckNewEntity(block, positionWorld, GameSession.PlayerName);
        if (error != null)
        {
            Spacebox.Game.GUI.Chat.Write(error, Color4.Red);
            Spacebox.Game.Player.PlacementRefund.Refund(Spacebox.Game.GUI.PanelUI.Player, block.Id);
            return;
        }

        Create(sector, positionWorld, block, GameSession.PlayerName);
        Spacebox.Game.Protection.ClaimPlacement.OnPlacedLocally(block);
    }

    private static SpaceEntity Create(Sector sector, Vector3 positionWorld, Block block, string owner)
    {
        var entity = sector.CreateNewEntity(positionWorld);
        entity.CreateFirstBlock(block);
        Spacebox.Game.Protection.ClaimPlacement.AssignOwner(entity, Vector3i.Zero, owner);

        NetworkManager.Instance.SendToClients(new EntityCreatedMessage
        {
            Sector = sector.PositionIndex,
            EntityId = entity.EntityID,
            PositionWorld = positionWorld,
            BlockId = block.Id,
            Direction = (byte)block.Direction,
            Rotation = (byte)block.Rotation
        });
        return entity;
    }

    private static void OnCreateRequest(int sender, EntityCreateRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || !GameAssets.Blocks.ContainsKey(message.BlockId)) return;
        if (sender != network.LocalPeerId && !Players.PlayerGameModeService.RulesOf(PlayerRegistry.GetName(sender)).PlaceBlocks) return;
        if (!WorldEditNetwork.ChargePlacement(sender, message.BlockId)) return;

        var sector = World.SectorAt(message.PositionWorld);
        if (sector == null || !sector.BoundingBox.Contains(message.PositionWorld) || !IsInReach(sender, message.PositionWorld))
        {
            WorldEditNetwork.SendRefund(sender, message.BlockId);
            return;
        }

        var block = CreateBlock(message.BlockId, message.Direction, message.Rotation);
        string owner = PlayerRegistry.GetName(sender);
        string error = Spacebox.Game.Protection.ClaimPlacement.CheckNewEntity(block, message.PositionWorld, owner);
        if (error != null)
        {
            WorldEditNetwork.SendRefund(sender, message.BlockId);
            ChatService.SendToPlayer(owner, error, Color4.Red);
            return;
        }

        Create(sector, message.PositionWorld, block, owner);
    }

    private static bool IsInReach(int peer, Vector3 positionWorld)
    {
        var player = Combat.PlayerHitboxes.FindPlayer(peer);
        return player != null && float.IsFinite(positionWorld.X + positionWorld.Y + positionWorld.Z) &&
               Vector3.DistanceSquared(player.Position, positionWorld) <= WorldEditNetwork.MaxEditDistance * WorldEditNetwork.MaxEditDistance;
    }

    private static void OnCreated(int sender, EntityCreatedMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var world = World.Instance;
        if (world == null || !world.TryGetSector(message.Sector, out var sector) || world.TryFindEntity(message.EntityId, out _)) return;

        var entity = sector.CreateNewEntity(message.PositionWorld, message.EntityId);
        entity.CreateFirstBlock(CreateBlock(message.BlockId, message.Direction, message.Rotation));
    }

    private static Block CreateBlock(short id, byte direction, byte rotation)
    {
        var block = GameAssets.CreateBlockFromId(id);
        block.Direction = (Direction)direction;
        block.Rotation = (Rotation)rotation;
        return block;
    }
}
