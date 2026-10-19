namespace Spacebox.Game.Ships;

public sealed class ShipPhysicsSettings
{
    public float MaxSpeed { get; set; } = 20f;
    public float MaxAngularSpeed { get; set; } = 1.5f;
    public float LinearDamping { get; set; } = 0.25f;
    public float AngularDamping { get; set; } = 0.03f;
    public float LinearSnap { get; set; } = 0.025f;
    public float AngularSnap { get; set; } = 0.003f;
    public float MaxStep { get; set; } = 0.05f;
    public float BoardDistance { get; set; } = 2.5f;
    public float LeaveDistance { get; set; } = 5f;
    public float CollisionBounce { get; set; } = 0.2f;

    public static ShipPhysicsSettings Current { get; set; } = new ShipPhysicsSettings();

    public void Validate()
    {
        MaxSpeed = Math.Max(0.1f, MaxSpeed);
        MaxAngularSpeed = Math.Max(0.01f, MaxAngularSpeed);
        LinearDamping = Math.Max(0f, LinearDamping);
        AngularDamping = Math.Max(0f, AngularDamping);
        LinearSnap = Math.Max(0f, LinearSnap);
        AngularSnap = Math.Max(0f, AngularSnap);
        MaxStep = Math.Clamp(MaxStep, 0.001f, 0.25f);
        BoardDistance = Math.Max(0.5f, BoardDistance);
        LeaveDistance = Math.Max(BoardDistance + 0.5f, LeaveDistance);
        CollisionBounce = Math.Clamp(CollisionBounce, 0f, 1f);
    }
}
