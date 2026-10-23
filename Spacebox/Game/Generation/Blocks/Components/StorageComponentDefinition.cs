namespace Spacebox.Game.Generation.Blocks.Components;

public sealed class StorageComponentDefinition : BlockComponentDefinition
{
    public const string ComponentName = "storage";

    public StorageComponentDefinition(Vector2Byte size, string storageName)
    {
        Size = new Vector2Byte(Math.Max((byte)1, size.X), Math.Max((byte)1, size.Y));
        StorageName = storageName ?? "";
    }

    public Vector2Byte Size { get; }
    public string StorageName { get; }

    public override string Name => ComponentName;
    public override BlockComponentFeatures Features => BlockComponentFeatures.Container;
    public override BlockComponent Create() => new StorageComponent(this);
}
