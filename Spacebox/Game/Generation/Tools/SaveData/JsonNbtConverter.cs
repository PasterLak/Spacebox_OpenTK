using System.Text.Json;
using System.Text.Json.Nodes;
using SharpNBT;

namespace Spacebox.Game.Generation.Tools.SaveData;

public static class JsonNbtConverter
{
    public const string RootName = "save";
    public const string RootKey = "$root";
    public const string WrapperKey = "$w";
    public const string WrappedValueKey = "$v";

    public static CompoundTag ToNbt(JsonNode root)
    {
        if (root is JsonObject obj) return ToCompound(obj, RootName);

        var wrapper = new CompoundTag(RootName);
        var value = ToTag(root, RootKey);
        if (value != null) wrapper.Add(value);
        return wrapper;
    }

    public static JsonNode ToJson(CompoundTag root)
    {
        if (root == null) return null;
        if (root.Count == 1 && root.TryGetValue(RootKey, out Tag wrapped)) return FromTag(wrapped);
        return FromTag(root);
    }

    private static Tag ToTag(JsonNode node, string name)
    {
        return node switch
        {
            null => null,
            JsonObject obj => ToCompound(obj, name),
            JsonArray array => ToList(array, name),
            JsonValue value => ToValueTag(value, name),
            _ => null
        };
    }

    private static CompoundTag ToCompound(JsonObject obj, string name)
    {
        var compound = new CompoundTag(name);
        foreach (var pair in obj)
        {
            var tag = ToTag(pair.Value, pair.Key);
            if (tag != null) compound.Add(tag);
        }
        return compound;
    }

    private static ListTag ToList(JsonArray array, string name)
    {
        if (array.Count == 0) return new ListTag(name, TagType.End);

        bool anyFraction = false;
        bool allNumbers = true;
        foreach (var item in array)
        {
            if (item is JsonValue value && value.GetValueKind() == JsonValueKind.Number)
            {
                if (!value.TryGetValue(out long _)) anyFraction = true;
            }
            else
            {
                allNumbers = false;
            }
        }

        if (allNumbers)
        {
            var numbers = new ListTag(name, anyFraction ? TagType.Double : TagType.Long);
            foreach (var item in array)
            {
                var value = (JsonValue)item;
                numbers.Add(anyFraction ? new DoubleTag(null, value.GetValue<double>()) : new LongTag(null, value.GetValue<long>()));
            }
            return numbers;
        }

        var tags = new List<Tag>(array.Count);
        TagType? shared = null;
        bool uniform = true;
        foreach (var item in array)
        {
            var tag = ToTag(item, null);
            tags.Add(tag);
            if (tag == null) { uniform = false; continue; }
            if (shared == null) shared = tag.Type;
            else if (shared != tag.Type) uniform = false;
        }

        if (uniform && shared != null)
        {
            var list = new ListTag(name, shared.Value);
            foreach (var tag in tags) list.Add(tag);
            return list;
        }

        var wrappedList = new ListTag(name, TagType.Compound);
        foreach (var item in array) wrappedList.Add(Wrap(item));
        return wrappedList;
    }

    private static CompoundTag Wrap(JsonNode item)
    {
        var wrapper = new CompoundTag(null) { new ByteTag(WrapperKey, (byte)1) };
        var value = ToTag(item, WrappedValueKey);
        if (value != null) wrapper.Add(value);
        return wrapper;
    }

    private static Tag ToValueTag(JsonValue value, string name)
    {
        switch (value.GetValueKind())
        {
            case JsonValueKind.String:
                return new StringTag(name, value.GetValue<string>());
            case JsonValueKind.True:
                return new ByteTag(name, (byte)1);
            case JsonValueKind.False:
                return new ByteTag(name, (byte)0);
            case JsonValueKind.Number:
                if (value.TryGetValue(out long whole)) return new LongTag(name, whole);
                return new DoubleTag(name, value.GetValue<double>());
            default:
                return null;
        }
    }

    private static JsonNode FromTag(Tag tag)
    {
        switch (tag)
        {
            case CompoundTag compound when compound.TryGetValue(WrapperKey, out Tag _):
                return compound.TryGetValue(WrappedValueKey, out Tag wrapped) ? FromTag(wrapped) : null;
            case CompoundTag compound:
                var obj = new JsonObject();
                foreach (var child in compound) obj[child.Name] = FromTag(child);
                return obj;
            case ListTag list:
                var array = new JsonArray();
                foreach (var child in list) array.Add(FromTag(child));
                return array;
            case StringTag text:
                return JsonValue.Create(text.Value);
            case ByteTag flag:
                return JsonValue.Create(flag.Value != 0);
            case ShortTag number:
                return JsonValue.Create((long)number.Value);
            case IntTag number:
                return JsonValue.Create((long)number.Value);
            case LongTag number:
                return JsonValue.Create(number.Value);
            case FloatTag number:
                return JsonValue.Create((double)number.Value);
            case DoubleTag number:
                return JsonValue.Create(number.Value);
            case IntArrayTag ints:
                return new JsonArray(ints.ToArray().Select(v => (JsonNode)JsonValue.Create((long)v)).ToArray());
            case LongArrayTag longs:
                return new JsonArray(longs.ToArray().Select(v => (JsonNode)JsonValue.Create(v)).ToArray());
            default:
                return null;
        }
    }
}
