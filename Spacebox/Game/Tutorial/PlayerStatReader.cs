using System.Collections;
using System.Reflection;
using Spacebox.Game.Player;

namespace Spacebox.Game.Tutorial;

public static class PlayerStatReader
{
    private sealed class Accessor
    {
        public PropertyInfo Property;
        public PropertyInfo Count;
    }

    private static readonly Dictionary<string, Accessor> _accessors = new Dictionary<string, Accessor>(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Names
    {
        get
        {
            var names = new List<string>();
            foreach (var property in typeof(PlayerStatistics).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Create(property) != null) names.Add(property.Name);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }
    }

    public static bool IsKnown(string stat)
    {
        return Find(stat) != null;
    }

    public static double Read(PlayerStatistics statistics, string stat)
    {
        if (statistics == null) return 0;

        var accessor = Find(stat);
        if (accessor == null) return 0;

        var value = accessor.Property.GetValue(statistics);
        if (value == null) return 0;
        if (accessor.Count != null) return Convert.ToDouble(accessor.Count.GetValue(value));

        return value switch
        {
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            _ => 0
        };
    }

    private static Accessor Find(string stat)
    {
        if (string.IsNullOrWhiteSpace(stat)) return null;
        if (_accessors.TryGetValue(stat, out var cached)) return cached;

        var accessor = Create(typeof(PlayerStatistics).GetProperty(stat, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
        _accessors[stat] = accessor;
        return accessor;
    }

    private static Accessor Create(PropertyInfo property)
    {
        if (property == null) return null;

        var type = property.PropertyType;
        if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double))
            return new Accessor { Property = property };

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var count = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (count != null && count.PropertyType == typeof(int)) return new Accessor { Property = property, Count = count };
        }

        return null;
    }
}
