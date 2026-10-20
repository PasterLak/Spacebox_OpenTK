namespace Spacebox.Game.Generation.Blocks;

public static class BlockDamage
{
    public static byte Apply(byte durability, byte damage)
    {
        int left = durability - damage;
        return (byte)(left < 0 ? 0 : left);
    }

    public static byte Repair(byte durability, byte amount, byte max)
    {
        int repaired = durability + amount;
        return (byte)(repaired > max ? Math.Max((int)durability, (int)max) : repaired);
    }
}
