namespace Spacebox.Game.Resource;

public class ThrusterJSON
{
    public float Force { get; set; } = 20f;
    public int Power { get; set; } = 8;
}

public class ShipConsoleJSON
{
    public int Buttons { get; set; } = 8;
    public int Power { get; set; } = 15;
    public bool Weapons { get; set; }
    public bool Flight { get; set; } = true;
}

public class CannonJSON
{
    public string Projectile { get; set; } = "";
    public int ReloadTime { get; set; } = 1000;
    public string ShotSound { get; set; } = "ar";
    public float BarrelLength { get; set; } = 1f;
    public float AimCone { get; set; } = 60f;
    public float TurnSpeed { get; set; } = 180f;
    public int Power { get; set; } = 15;
}

public class ShipCameraJSON
{
    public int Power { get; set; } = 15;
}
