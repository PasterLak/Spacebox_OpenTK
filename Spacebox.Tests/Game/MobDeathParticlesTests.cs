using Spacebox.Game.Mobs;

namespace Spacebox.Tests;

public class MobDeathParticlesTests
{
    [Fact]
    public void MissingSectionHasNoParticles()
    {
        var settings = MobDeathParticles.Sanitize(null);

        Assert.Equal(0, settings.Count);
    }

    [Fact]
    public void ValuesAreClamped()
    {
        var settings = MobDeathParticles.Sanitize(new MobDeathParticlesJSON { Count = 5000, Speed = -1f, Size = 0f, Life = 100f, Radius = -2f });

        Assert.Equal(MobDeathParticles.MaxCount, settings.Count);
        Assert.Equal(0f, settings.Speed);
        Assert.Equal(0.01f, settings.Size);
        Assert.Equal(5f, settings.Life);
        Assert.Equal(0f, settings.Radius);
    }

    [Fact]
    public void ColorIsKept()
    {
        var settings = MobDeathParticles.Sanitize(new MobDeathParticlesJSON { Color = new Engine.Color3Byte(140, 70, 220), Count = 45 });

        Assert.Equal(140, settings.Color.R);
        Assert.Equal(70, settings.Color.G);
        Assert.Equal(220, settings.Color.B);
        Assert.Equal(45, settings.Count);
    }
}
