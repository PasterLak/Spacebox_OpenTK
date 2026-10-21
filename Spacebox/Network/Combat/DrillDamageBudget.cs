using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Network.Combat;

public sealed class DrillDamageBudget
{
    public const float MaxDistance = 8f;
    public const int MaxDamagePerSecond = 60;
    private const float Window = 1f;

    private readonly Dictionary<int, (float Start, int Damage)> _budgets = new Dictionary<int, (float, int)>();

    public int Consume(int attacker, Vector3 target, int damage)
    {
        if (damage <= 0) return 0;

        var astronaut = PlayerHitboxes.FindPlayer(attacker);
        if (astronaut == null) return 0;
        if (Vector3.DistanceSquared(astronaut.Position, target) > MaxDistance * MaxDistance) return 0;

        return Consume(attacker, damage, Time.Total);
    }

    public int Consume(int attacker, int damage, float now)
    {
        if (!_budgets.TryGetValue(attacker, out var budget) || now - budget.Start >= Window || now < budget.Start)
            budget = (now, 0);

        int allowed = Math.Min(damage, MaxDamagePerSecond - budget.Damage);
        if (allowed <= 0) return 0;

        _budgets[attacker] = (budget.Start, budget.Damage + allowed);
        return allowed;
    }

    public static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
