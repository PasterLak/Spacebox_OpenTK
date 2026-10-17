
namespace Spacebox.Game.Player;


public class ProtectionType
{
    public static readonly ProtectionType None = new ProtectionType("None", 0f);
    public static readonly ProtectionType Light = new ProtectionType("Light", 0.25f);
    public static readonly ProtectionType Medium = new ProtectionType("Medium", 0.5f);
    public static readonly ProtectionType Heavy = new ProtectionType("Heavy", 0.75f);
    public static readonly ProtectionType Full = new ProtectionType("Full", 1f);
    public string Name { get; }
    public float DamageReduction { get; }
    private ProtectionType(string name, float damageReduction)
    {
        Name = name;
        DamageReduction = damageReduction;
    }
}
