namespace Spacebox.Game.Generation.Blocks;

public static class BlockDamage
{
    public static byte Apply(byte durability, byte damage)
    {
        int left = durability - damage;
        return (byte)(left < 0 ? 0 : left);
    }
}
