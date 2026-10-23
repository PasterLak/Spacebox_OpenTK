using Engine;
using Engine.Physics;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Player.Targeting;

public static class BodyHighlight
{
    private const int Segments = 32;
    private const float RingScale = 1.35f;
    private const float Thickness = 0.025f;

    private static readonly Color4 DrillColor = new Color4(1f, 0.75f, 0.3f, 0.9f);
    private static readonly Color4 GiftColor = new Color4(0.45f, 1f, 0.5f, 0.9f);

    public static void Update(LocalAstronaut player, Item heldItem)
    {
        var physics = World.Instance?.Physics;
        if (player == null || physics == null || !player.CanMove || Camera.Main == null) return;

        float giftRange = Spacebox.Network.Players.ItemGiftNetwork.MaxDistance;
        float range = BodyHighlightRules.Range(heldItem, giftRange);
        if (range <= 0f) return;

        var ray = new Ray(player.Position, player.Front, range);
        if (!physics.Raycast(ray, player.Body, out var hit) || hit.Body == null) return;

        var kind = BodyHighlightRules.Resolve(hit.Body.Owner, heldItem, hit.Distance, giftRange, Engine.Networking.NetworkManager.Instance.IsActive);
        if (kind == BodyHighlightKind.None || IsBehindBlock(player, hit.Distance)) return;

        var color = kind == BodyHighlightKind.Drill ? DrillColor : GiftColor;
        if (hit.Body.Owner is Node3D node)
        {
            Spacebox.Game.Rendering.BodyOutlineRenderer.Request(node, color);
            return;
        }

        float radius = hit.Body.HitRadius;
        var towardViewer = (player.Position - hit.Body.Center).LengthSquared > 1e-6f ? (player.Position - hit.Body.Center).Normalized() : -player.Front;
        DrawRing(hit.Body.Center + towardViewer * radius, radius * RingScale, player.Front, kind == BodyHighlightKind.Drill ? DrillColor : GiftColor);
    }

    private static bool IsBehindBlock(LocalAstronaut player, float distance)
    {
        var sector = World.CurrentSector;
        return sector != null && sector.Raycast(new Ray(player.Position, player.Front, distance), out var block) && block.block != null;
    }

    private static void DrawRing(Vector3 center, float radius, Vector3 viewDirection, Color4 color)
    {
        var forward = viewDirection.LengthSquared > 1e-6f ? viewDirection.Normalized() : -Vector3.UnitZ;
        var helper = MathF.Abs(forward.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX;
        var right = Vector3.Cross(forward, helper).Normalized();
        var up = Vector3.Cross(right, forward);

        var previous = center + right * radius;
        for (int i = 1; i <= Segments; i++)
        {
            float angle = MathHelper.TwoPi * i / Segments;
            var point = center + (right * MathF.Cos(angle) + up * MathF.Sin(angle)) * radius;
            WorldLines.DrawLine(previous, point, color, Thickness);
            previous = point;
        }
    }
}
