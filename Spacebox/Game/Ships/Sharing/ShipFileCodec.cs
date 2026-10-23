using System.IO.Compression;
using OpenTK.Mathematics;
using SharpNBT;
using Engine.Utils;

namespace Spacebox.Game.Ships.Sharing;

public static class ShipFileCodec
{
    public const long MaxUncompressedBytes = 64L * 1024 * 1024;
    private const string RootName = "ship";

    public static CompoundTag ToTag(ShipFile ship)
    {
        var root = new CompoundTag(RootName)
        {
            new IntTag("format", ShipFile.FormatVersion),
            new StringTag("id", ship.Id ?? ""),
            new StringTag("name", ship.Name ?? ""),
            new StringTag("author", ship.Author ?? ""),
            new StringTag("description", ship.Description ?? ""),
            new StringTag("game_version", ship.GameVersion ?? ""),
            new StringTag("created", ship.Created ?? ""),
            new IntTag("blocks", ship.Blocks),
            new IntArrayTag("bounds", new[] { ship.Min.X, ship.Min.Y, ship.Min.Z, ship.Max.X, ship.Max.Y, ship.Max.Z })
        };

        var gameSets = new ListTag("gamesets", TagType.String);
        foreach (var id in ship.GameSets) gameSets.Add(new StringTag(null, id));
        root.Add(gameSets);

        if (ship.Picture != null && ship.Picture.Length > 0) root.Add(new ByteArrayTag("picture", ship.Picture));

        var entity = new CompoundTag("entity");
        if (ship.Entity != null && ship.Entity.TryGetValue("chunks", out ListTag chunks)) entity.Add(Clone(chunks));
        root.Add(entity);
        return root;
    }

    public static ShipFile FromTag(CompoundTag root)
    {
        if (root == null || !root.TryGetValue<CompoundTag>("entity", out var entity)) throw new InvalidDataException("This is not a ship file");
        if (Int(root, "format") > ShipFile.FormatVersion) throw new InvalidDataException("The ship was saved by a newer game version");

        var ship = new ShipFile
        {
            Id = Text(root, "id"),
            Name = Text(root, "name"),
            Author = Text(root, "author"),
            Description = Text(root, "description"),
            GameVersion = Text(root, "game_version"),
            Created = Text(root, "created"),
            Blocks = Int(root, "blocks"),
            Picture = root.TryGetValue<ByteArrayTag>("picture", out var picture) ? picture.ToArray() : null,
            Entity = entity
        };

        if (root.TryGetValue<IntArrayTag>("bounds", out var bounds) && bounds.Count == 6)
        {
            ship.Min = new Vector3i(bounds[0], bounds[1], bounds[2]);
            ship.Max = new Vector3i(bounds[3], bounds[4], bounds[5]);
        }

        if (root.TryGetValue<ListTag>("gamesets", out var gameSets))
            foreach (var tag in gameSets)
                if (tag is StringTag text) ship.GameSets.Add(text.Value);

        return ship;
    }

    public static byte[] ToBytes(CompoundTag tag)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
        using (var writer = new TagWriter(gzip, FormatOptions.Java))
        {
            writer.WriteTag(tag);
        }
        return output.ToArray();
    }

    public static CompoundTag FromBytes(byte[] data, long maxUncompressedBytes = MaxUncompressedBytes)
    {
        if (data == null || data.Length == 0) throw new InvalidDataException("The ship file is empty");

        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var plain = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (plain.Length + read > maxUncompressedBytes) throw new InvalidDataException("The ship file is too large");
            plain.Write(buffer, 0, read);
        }

        plain.Position = 0;
        using var reader = new TagReader(plain, FormatOptions.Java);
        return reader.ReadTag<CompoundTag>();
    }

    public static void Write(string path, ShipFile ship)
    {
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        byte[] data = ToBytes(ToTag(ship));
        AtomicFile.Write(path, temp => File.WriteAllBytes(temp, data));
    }

    public static ShipFile Read(string path) => FromTag(FromBytes(File.ReadAllBytes(path)));

    public static CompoundTag Clone(CompoundTag tag) => FromBytes(ToBytes(tag));

    private static ListTag Clone(ListTag list)
    {
        var wrapper = new CompoundTag("wrap") { list };
        var copy = Clone(wrapper);
        var result = copy.Get<ListTag>(list.Name);
        copy.Remove(result);
        return result;
    }

    private static string Text(CompoundTag root, string key) => root.TryGetValue<StringTag>(key, out var tag) ? tag.Value ?? "" : "";

    private static int Int(CompoundTag root, string key) => root.TryGetValue<IntTag>(key, out var tag) ? tag.Value : 0;
}
