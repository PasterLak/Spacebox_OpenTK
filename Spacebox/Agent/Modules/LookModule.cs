using Engine.Web;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Agent.Modules;

public sealed class LookModule : IAgentModule
{
    public void Map(AgentRouter router)
    {
        router.Get("/look", "Block under the crosshair (?distance=64)", request =>
        {
            var player = AgentGame.RequirePlayer();
            float distance = AgentQuery.Float(request, "distance", AgentGame.DefaultRayDistance);
            return AgentJson.Response(new
            {
                position = AgentGame.Vec(player.Position),
                front = AgentGame.Vec(player.Front),
                target = AgentGame.TryRaycast(player, distance, out var hit) ? AgentGame.DescribeHit(player, hit) : null
            });
        });

        router.Post("/look", "Turn the camera: ?x=&y=&z= (look at a world point), ?yaw=&pitch= (absolute degrees) or ?dyaw=&dpitch= (relative)", request =>
        {
            var player = AgentGame.RequirePlayer();
            var front = ResolveFront(request, player);
            if (front.LengthSquared < 1e-8f) throw new AgentRequestException("The look direction is zero");

            Face(player, front.Normalized());
            AgentGame.TryRaycast(player, AgentGame.DefaultRayDistance, out var hit);
            return AgentJson.Response(new
            {
                front = AgentGame.Vec(player.Front),
                yaw = AgentGame.Round(AgentGame.Yaw(player.Front)),
                pitch = AgentGame.Round(AgentGame.Pitch(player.Front)),
                target = hit.block != null ? AgentGame.DescribeHit(player, hit) : null
            });
        });
    }

    public static void Face(LocalAstronaut player, Vector3 front)
    {
        var up = MathF.Abs(Vector3.Dot(front, Vector3.UnitY)) > 0.999f ? -Vector3.UnitZ : Vector3.UnitY;
        player.SetRotation(GravityWalker.LookRotation(front, up));
    }

    private static Vector3 ResolveFront(HttpRequest request, LocalAstronaut player)
    {
        if (AgentQuery.HasVector(request)) return AgentQuery.Vector(request) - player.Position;

        float yaw = AgentGame.Yaw(player.Front);
        float pitch = AgentGame.Pitch(player.Front);
        if (AgentQuery.Has(request, "yaw") || AgentQuery.Has(request, "pitch"))
            return AgentGame.FrontFromAngles(AgentQuery.Float(request, "yaw", yaw), AgentQuery.Float(request, "pitch", pitch));
        if (AgentQuery.Has(request, "dyaw") || AgentQuery.Has(request, "dpitch"))
            return AgentGame.FrontFromAngles(yaw + AgentQuery.Float(request, "dyaw", 0f), pitch + AgentQuery.Float(request, "dpitch", 0f));

        throw new AgentRequestException("Pass x,y,z or yaw/pitch or dyaw/dpitch");
    }
}
