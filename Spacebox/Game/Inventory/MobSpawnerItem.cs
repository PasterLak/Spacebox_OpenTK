namespace Spacebox.Game;

public class MobSpawnerItem : Item
{
    public string MobId = "";
    public float UseCooldown = 0.5f;

    public MobSpawnerItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
    {
    }
}
