using System.Reflection;
using System.Text.Json.Serialization;
using Spacebox.Editor.Data;
using Xunit;

namespace Spacebox.Tests.Editor;

public class FieldHelpTests
{
    private static string HelpFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Spacebox", FieldHelp.RelativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return Path.Combine(AppContext.BaseDirectory, FieldHelp.RelativePath);
    }

    [Fact]
    public void EverySettingInTheEditorHasATooltip()
    {
        var help = FieldHelp.Load(HelpFile());
        var missing = new List<string>();
        foreach (var type in JsonSchema.Reachable(GameSetCatalog.SchemaRoots()))
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0) continue;
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
                string name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                if (!help.Has(type, name)) missing.Add($"{type.Name}.{name} ({property.PropertyType.Name})");
            }
        }

        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "spacebox_missing_help.txt"), missing);
        Assert.True(missing.Count == 0, $"{missing.Count} settings without a tooltip in {FieldHelp.RelativePath}:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void InheritedSettingsUseTheBaseTypeText()
    {
        var help = new FieldHelp(new Dictionary<string, Dictionary<string, string>> { ["ItemJSON"] = new Dictionary<string, string> { ["Name"] = "Shown name" } });

        Assert.Equal("Shown name", help.For(typeof(Spacebox.Game.Resource.WeaponItemJSON), "name"));
        Assert.Null(help.For(typeof(Spacebox.Game.Resource.WeaponItemJSON), "Unknown"));
    }
}
