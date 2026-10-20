using System.Text;

namespace Spacebox.Game;

public class RepairToolItem : Item, IItemTooltip
{
    public float RepairTime { get; init; } = 1f;
    public float PowerPerSecond { get; init; } = 2f;
    public float Range { get; init; } = 5f;

    public RepairToolItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
    {
    }

    public float GetRepairRate(int maxDurability)
    {
        return maxDurability <= 0 ? 0f : maxDurability / MathF.Max(RepairTime, 0.01f);
    }

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nFull repair: {RepairTime:0.##}s");
        text.Append($"\nPower usage: {PowerPerSecond:0.#}/s");
        text.Append($"\nRange: {Range:0.#} blocks");
    }
}
