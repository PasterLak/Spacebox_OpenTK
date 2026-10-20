using System.Text;
using System.Text.Json;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class GeneratorExplosionTests
{
    private static GeneratorBlockData Create(ExplosiveJSON explosive)
    {
        return new GeneratorBlockData(new BlockData("reactor", "generator"), new PowerGeneratorJSON { Power = 180 }, explosive);
    }

    [Fact]
    public void GeneratorWithoutExplosive_DoesNotExplode()
    {
        var data = Create(null);

        Assert.Null(data.Explosion);
        var text = new StringBuilder();
        data.AppendTooltip(text);
        Assert.Equal("", text.ToString());
    }

    [Fact]
    public void GeneratorWithExplosive_HasSmallBlast()
    {
        var data = Create(new ExplosiveJSON { Radius = 4, Penetration = 35, Damage = 150 });

        Assert.NotNull(data.Explosion);
        Assert.Equal(4f, data.Explosion.Radius);
        Assert.Equal(35, data.Explosion.Penetration);
        Assert.Equal(0f, data.Explosion.GetDamage(4.5f));
        Assert.True(data.Explosion.GetDamage(1f) > data.Explosion.GetDamage(3f));

        var text = new StringBuilder();
        data.AppendTooltip(text);
        Assert.Contains("Explodes when destroyed", text.ToString());
    }

    [Fact]
    public void GeneratorJsonWithoutExplosiveSection_DoesNotExplode()
    {
        var json = JsonSerializer.Deserialize<BlockJSON>("{\"ID\": \"cobalt\", \"Type\": \"generator\", \"Generator\": {\"Power\": 50}}");

        var data = new GeneratorBlockData(new BlockData("cobalt", "generator"), json.Generator, json.Explosive);

        Assert.Null(data.Explosion);
    }

    [Fact]
    public void GeneratorJsonWithExplosiveSection_Explodes()
    {
        var json = JsonSerializer.Deserialize<BlockJSON>("{\"ID\": \"reactor\", \"Type\": \"generator\", \"Explosive\": {\"Radius\": 3}}");

        var data = new GeneratorBlockData(new BlockData("reactor", "generator"), json.Generator, json.Explosive);

        Assert.NotNull(data.Explosion);
        Assert.Equal(3f, data.Explosion.Radius);
    }

    [Fact]
    public void BombData_IsItsOwnExplosion()
    {
        var bomb = new ExplosiveBlockData(new BlockData("bomb", "explosive"), new ExplosiveJSON());

        Assert.Same(bomb, ((IExplosiveBlockData)bomb).Explosion);
    }
}

public class DefaultReactorBalanceTests
{
    private const int BaseHealth = 100;
    private const float MaxRadius = 3f;

    private static JsonElement[] Load(string file)
    {
        string path = Path.Combine(Spacebox.Tests.Versioning.WorldToolkitTests.DefaultGameSet(), "Default", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        return document.RootElement.EnumerateArray().Select(e => e.Clone()).ToArray();
    }

    private static IEnumerable<JsonElement> ExplosiveGenerators()
    {
        return Load("blocks.json").Where(b => b.TryGetProperty("Type", out var type) && type.GetString() == "generator" && b.TryGetProperty("Explosive", out _));
    }

    [Fact]
    public void NuclearGenerators_AreExplosive()
    {
        var ids = ExplosiveGenerators().Select(b => b.GetProperty("ID").GetString()).ToList();

        Assert.Contains("standard_nuclear_power_block", ids);
        Assert.Contains("nuclear_power_block", ids);
        Assert.DoesNotContain("cobalt_power_block", ids);
        Assert.DoesNotContain("powerful_cobalt_power_block", ids);
    }

    [Fact]
    public void ReactorBlast_IsSmall()
    {
        foreach (var block in ExplosiveGenerators())
            Assert.True(block.GetProperty("Explosive").GetProperty("Radius").GetSingle() <= MaxRadius, block.GetProperty("ID").GetString());
    }

    [Fact]
    public void FullTitaniumArmor_SurvivesAReactorBlastAtPointBlank()
    {
        var titanium = Load("items.json").Where(i => i.GetProperty("ID").GetString().StartsWith("titanium_") && i.TryGetProperty("ExplosionProtection", out _)).ToList();
        float protection = ArmorProtection.Combine(titanium.Sum(i => ArmorProtection.FromPercent(i.GetProperty("ExplosionProtection").GetSingle())));
        int health = BaseHealth + titanium.Sum(i => i.TryGetProperty("Health", out var h) ? h.GetInt32() : 0);

        foreach (var block in ExplosiveGenerators())
        {
            int damage = ArmorProtection.Apply(block.GetProperty("Explosive").GetProperty("Damage").GetInt32(), protection);
            Assert.True(damage < health, $"{block.GetProperty("ID").GetString()}: {damage} damage vs {health} health");
        }
    }
}
