using Spacebox.Editor.Canvas.Inspector;

namespace Spacebox.Tests.Editor;

public sealed class EditorScopesTests
{
    [Theory]
    [InlineData("Blocks/Crates/safe/safe.json", EditorScopes.Blocks)]
    [InlineData("Blocks/Crates/safe/blueprint.json", EditorScopes.Crafting)]
    [InlineData("Items/Tools/drill/recipes.json", EditorScopes.Recipes)]
    [InlineData("Items/Tools/drill/drill.json", EditorScopes.Items)]
    [InlineData("Recipes/furnace.json", EditorScopes.Recipes)]
    [InlineData("Crafting/categories.json", EditorScopes.Crafting)]
    [InlineData("Generation/Asteroids/light.json", EditorScopes.Asteroids)]
    [InlineData("Generation/generator.json", EditorScopes.Generator)]
    [InlineData("Tutorials/basics.json", EditorScopes.Tutorials)]
    [InlineData("disassembly.json", EditorScopes.Disassembly)]
    [InlineData("config.json", EditorScopes.Config)]
    [InlineData("notes/other.json", EditorScopes.Generic)]
    public void ForPath_PicksTheScopeOfTheContentKind(string path, string expected)
    {
        Assert.Equal(expected, EditorScopes.ForPath(path));
    }
}
