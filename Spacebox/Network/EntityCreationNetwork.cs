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

        var entity = sector.CreateNewEntity(positionWorld);
        entity.CreateFirstBlock(block);

        network.SendToClients(new EntityCreatedMessage
        {
            Sector = sector.PositionIndex,
            EntityId = entity.EntityID,
            PositionWorld = positionWorld,
            BlockId = block.Id,
            Direction = (byte)block.Direction,
            Rotation = (byte)block.Rotation
        });
    }

    private static void OnCreateRequest(int sender, EntityCreateRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer || !GameAssets.Blocks.ContainsKey(message.BlockId)) return;

        var sector = World.SectorAt(message.PositionWorld);
        if (sector == null || !sector.BoundingBox.Contains(message.PositionWorld)) return;

        PlaceNewEntity(sector, message.PositionWorld, CreateBlock(message.BlockId, message.Direction, message.Rotation));
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
