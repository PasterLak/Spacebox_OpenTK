using Spacebox.Game.Resource;

namespace Spacebox.Tests;

public class DnaReconstructorSoundsTests
{
    private static readonly string DefaultSounds = Path.Combine(AppContext.BaseDirectory, "Mods", "GameSets", "Default", "Sounds");

    [Fact]
    public void MissingSettingsUseDefaultSounds()
    {
        var data = new DnaReconstructorBlockData(new BlockData("dna_reconstructor", "dnareconstructor"), null, "default");

        Assert.Equal("dnaReconstructionLoop", data.Sounds.Loop);
        Assert.Equal("dnaReconstructionStage", data.Sounds.Stage);
        Assert.Equal("dnaReconstructionSpawn", data.Sounds.Spawn);
        Assert.Equal(1f, data.Sounds.Volume);
    }

    [Fact]
    public void NullSoundsSectionFallsBackToDefaults()
    {
        var settings = new DnaReconstructorJSON { Sounds = null };
        var data = new DnaReconstructorBlockData(new BlockData("dna_reconstructor", "dnareconstructor"), settings, "default");

        Assert.Equal("dnaReconstructionSpawn", data.Sounds.Spawn);
    }

    [Theory]
    [InlineData("dnaReconstructionLoop")]
    [InlineData("dnaReconstructionStage")]
    [InlineData("dnaReconstructionSpawn")]
    public void DefaultGameSetContainsSound(string name)
    {
        Assert.NotEmpty(Directory.GetFiles(DefaultSounds, name + ".wav", SearchOption.AllDirectories));
    }
}
