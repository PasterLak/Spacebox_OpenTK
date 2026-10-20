using SharpNBT;

namespace Spacebox.Game.Versioning;

public static class NbtFileEditor
{
    public static CompoundTag Read(string path)
    {
        return NbtFile.Read(path, FormatOptions.Java, CompressionType.GZip);
    }

    public static void Write(string path, CompoundTag tag)
    {
        NbtFile.Write(path, tag, FormatOptions.Java, CompressionType.GZip);
    }

    public static bool Edit(string path, Func<CompoundTag, bool> edit)
    {
        if (!File.Exists(path)) return false;

        var tag = Read(path);
        if (tag == null || !edit(tag)) return false;

        Write(path, tag);
        return true;
    }

    public static Tag Renamed(Tag tag, string name)
    {
        return tag switch
        {
            ByteTag t => new ByteTag(name, t.Value),
            ShortTag t => new ShortTag(name, t.Value),
            IntTag t => new IntTag(name, t.Value),
            LongTag t => new LongTag(name, t.Value),
            FloatTag t => new FloatTag(name, t.Value),
            DoubleTag t => new DoubleTag(name, t.Value),
            StringTag t => new StringTag(name, t.Value),
            ByteArrayTag t => new ByteArrayTag(name, t.ToArray()),
            IntArrayTag t => new IntArrayTag(name, t.ToArray()),
            LongArrayTag t => new LongArrayTag(name, t.ToArray()),
            ListTag t => CopyList(t, name),
            CompoundTag t => CopyCompound(t, name),
            _ => throw new NotSupportedException($"Cannot rename a {tag?.Type} tag")
        };
    }

    public static bool RenameKey(CompoundTag parent, string oldName, string newName)
    {
        if (parent == null || !parent.TryGetValue(oldName, out Tag tag) || parent.ContainsKey(newName)) return false;

        parent.Remove(oldName);
        parent.Add(Renamed(tag, newName));
        return true;
    }

    private static ListTag CopyList(ListTag list, string name)
    {
        var copy = new ListTag(name, list.ChildType);
        foreach (var child in list) copy.Add(Renamed(child, null));
        return copy;
    }

    private static CompoundTag CopyCompound(CompoundTag compound, string name)
    {
        var copy = new CompoundTag(name);
        foreach (var child in compound) copy.Add(Renamed(child, child.Name));
        return copy;
    }
}
