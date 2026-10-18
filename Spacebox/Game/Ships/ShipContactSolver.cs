using OpenTK.Mathematics;

namespace Spacebox.Game.Ships;

public static class ShipContactSolver
{
    public const float Friction = 0.3f;

    public static bool TrySolve(Vector3 relativeVelocity, Vector3 normal, float inverseMassA, float inverseMassB, float bounce, out Vector3 impulse)
    {
        impulse = Vector3.Zero;
        float normalSpeed = Vector3.Dot(relativeVelocity, normal);
        float inverseMass = inverseMassA + inverseMassB;
        if (normalSpeed >= 0f || inverseMass <= 0f) return false;

        float normalImpulse = -(1f + bounce) * normalSpeed / inverseMass;
        impulse = normal * normalImpulse;

        var tangent = relativeVelocity - normal * normalSpeed;
        float tangentSpeed = tangent.Length;
        if (tangentSpeed > 1e-5f)
        {
            float tangentImpulse = MathF.Min(tangentSpeed / inverseMass, Friction * normalImpulse);
            impulse -= tangent / tangentSpeed * tangentImpulse;
        }

        return true;
    }

    public static int GetSubsteps(float speed, float angularSpeed, float radius, float deltaTime, float maxTravel, int maxSubsteps)
    {
        float travel = (speed + angularSpeed * radius) * deltaTime;
        return Math.Clamp((int)MathF.Ceiling(travel / maxTravel), 1, maxSubsteps);
    }
}
