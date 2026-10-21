using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Player;

namespace Spacebox.Agent;

public static class AgentGame
{
    public const float DefaultRayDistance = 64f;

    public static LocalAstronaut Player => World.Instance?.Player;

    public static LocalAstronaut RequirePlayer()
    {
        return Player ?? throw new AgentRequestException("No world is loaded (start the game with --agent-world <name> or open a world)", 409);
    }

    public static float[] Vec(Vector3 v) => new[] { Round(v.X), Round(v.Y), Round(v.Z) };

    public static int[] Vec(Vector3i v) => new[] { v.X, v.Y, v.Z };

    public static float Round(float value) => MathF.Round(value, 3);

    public static float Yaw(Vector3 front) => MathHelper.RadiansToDegrees(MathF.Atan2(front.X, -front.Z));

    public static float Pitch(Vector3 front) => MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(front.Y, -1f, 1f)));

    public static Vector3 FrontFromAngles(float yawDegrees, float pitchDegrees)
    {
        float yaw = MathHelper.DegreesToRadians(yawDegrees);
        float pitch = MathHelper.DegreesToRadians(Math.Clamp(pitchDegrees, -89.9f, 89.9f));
        return new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(yaw) * MathF.Cos(pitch));
    }

    public static bool TryRaycast(LocalAstronaut player, float distance, out HitInfo hit)
    {
        hit = default;
        var sector = World.CurrentSector;
        return sector != null && sector.Raycast(new Ray(player.Position, player.Front, distance), out hit);
    }

    public static object DescribeHit(LocalAstronaut player, HitInfo hit)
    {
        float distance = Vector3.Distance(player.Position, hit.hitPosition);
        return new
        {
            block = DescribeBlock(hit.block),
            blockPosition = Vec(hit.blockPositionWorld),
            entityCell = Vec(hit.blockPositionEntity),
            entityId = hit.chunk?.SpaceEntity?.EntityID,
            hitPoint = Vec(hit.hitPosition),
            normal = Vec(hit.normalWorld),
            distance = Round(distance),
            usable = distance <= InteractiveBlock.InteractionDistance + 0.5f
        };
    }

    public static object DescribeBlock(Block block)
    {
        if (block == null) return null;

        var data = GameAssets.GetBlockDataById(block.Id);
        return new
        {
            id = block.Id,
            fullId = data?.Id_string ?? "",
            name = data?.Name ?? "",
            type = block.GetType().Name,
            air = block.IsAir,
            direction = block.Direction.ToString(),
            durability = block.Durability
        };
    }

    public static object DescribeItem(Item item, int count)
    {
        if (item == null) return null;
        return new { id = item.Id, fullId = item.Id_string, name = item.Name, type = item.GetType().Name, count };
    }
}
