using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Spacebox.Editor.Data;

public sealed class SchemaProperty
{
    public string Name { get; }
    public Type ClrType { get; }
    public JsonNode Default { get; }

    public SchemaProperty(string name, Type clrType, JsonNode defaultValue)
    {
        Name = name;
        ClrType = clrType;
        Default = defaultValue;
    }

    public JsonNode CreateValue()
    {
        if (Default != null) return Default.DeepClone();
        return JsonSchema.CreateEmpty(ClrType);
    }
}

public static class JsonSchema
{
    private static readonly Dictionary<Type, IReadOnlyList<SchemaProperty>> Cache = new Dictionary<Type, IReadOnlyList<SchemaProperty>>();
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { IncludeFields = false };

    public static IReadOnlyList<SchemaProperty> For(Type type)
    {
        if (type == null || IsSimple(type)) return Array.Empty<SchemaProperty>();
        if (Cache.TryGetValue(type, out var cached)) return cached;

        var result = new List<SchemaProperty>();
        object instance = TryCreate(type);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0) continue;
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            result.Add(new SchemaProperty(name, property.PropertyType, ReadDefault(property, instance)));
        }

        Cache[type] = result;
        return result;
    }

    public static SchemaProperty Find(Type type, string name)
    {
        var properties = For(type);
        for (int i = 0; i < properties.Count; i++)
        {
            if (string.Equals(properties[i].Name, name, StringComparison.OrdinalIgnoreCase)) return properties[i];
        }
        return null;
    }

    public static Type ElementType(Type collectionType)
    {
        if (collectionType == null) return null;
        if (collectionType.IsArray) return collectionType.GetElementType();
        if (IsDictionary(collectionType)) return null;
        if (collectionType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(collectionType))
        {
            var arguments = collectionType.GetGenericArguments();
            return arguments[^1];
        }
        return null;
    }

    public static bool IsDictionary(Type type)
    {
        return type != null && typeof(IDictionary).IsAssignableFrom(type);
    }

    public static Type DictionaryValueType(Type type)
    {
        return IsDictionary(type) && type.IsGenericType ? type.GetGenericArguments()[^1] : null;
    }

    public static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }

    public static JsonNode CreateEmpty(Type type)
    {
        if (type == null) return null;
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string)) return JsonValue.Create("");
        if (type == typeof(bool)) return JsonValue.Create(false);
        if (type.IsPrimitive || type == typeof(decimal)) return JsonValue.Create(0);
        if (type.IsEnum) return JsonValue.Create(Enum.GetNames(type).FirstOrDefault() ?? "");
        if (IsDictionary(type)) return new JsonObject();
        if (ElementType(type) != null && type != typeof(string)) return new JsonArray();

        var instance = TryCreate(type);
        if (instance == null) return new JsonObject();

        try
        {
            return JsonSerializer.SerializeToNode(instance, type, SerializerOptions) ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static JsonNode ReadDefault(PropertyInfo property, object instance)
    {
        if (instance == null) return null;

        try
        {
            var value = property.GetValue(instance);
            return value == null ? null : JsonSerializer.SerializeToNode(value, property.PropertyType, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private static object TryCreate(Type type)
    {
        if (type.IsAbstract || type.IsInterface) return null;
        if (type.IsValueType) return Activator.CreateInstance(type);
        if (type.GetConstructor(Type.EmptyTypes) == null) return null;

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }
}
