namespace Spacebox.Game.Generation.Blocks.Components;

[Flags]
public enum BlockComponentFeatures : byte
{
    None = 0,
    Container = 1 << 0,
    NetworkState = 1 << 1
}

public abstract class BlockComponentDefinition
{
    public abstract string Name { get; }
    public virtual BlockComponentFeatures Features => BlockComponentFeatures.None;
    public abstract BlockComponent Create();
}
