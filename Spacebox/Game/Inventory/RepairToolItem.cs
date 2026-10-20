using System.Text;

namespace Spacebox.Game;

public class RepairToolItem : Item, IItemTooltip
{
    public float RepairPerSecond { get; init; } = 4f;
    public float PowerPerSecond { get; init; } = 2f;
    public float Range { get; init; } = 5f;

    public RepairToolItem(byte stackSize, string name, float modelDepth) : base(stackSize, name, modelDepth)
    {
    }

    public void AppendTooltip(StringBuilder text)
    {
        text.Append($"\nRepairs: {RepairPerSecond:0.#} durability/s");
        text.Append($"\nPower usage: {PowerPerSecond:0.#}/s");
        text.Append($"\nRange: {Range:0.#} blocks");
    }
}
