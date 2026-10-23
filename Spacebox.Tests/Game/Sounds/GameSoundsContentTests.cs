using System.Text.Json.Nodes;
using Spacebox.Game;
using Spacebox.Game.GUI.Sounds;
using Spacebox.Game.Player;
using Xunit;

namespace Spacebox.Tests.Game.Sounds;

public class GameSoundsContentTests
{
    private static string SpaceboxFolder => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(DefaultGameSetContent.Folder)!)!)!;

    [Fact]
    public void EveryUiSoundHasAnExistingFile()
    {
        foreach (var sound in Enum.GetValues<UiSound>())
        {
            var spec = UiSoundCatalog.Get(sound);
            Assert.False(spec.IsEmpty, sound.ToString());
            Assert.InRange(spec.Volume, 0.05f, 1f);
            Assert.True(File.Exists(Path.Combine(SpaceboxFolder, spec.Clip)), spec.Clip);
        }
    }

    [Fact]
    public void BlockWorkingAndFinishedSoundsExist()
    {
        var sounds = DefaultGameSetContent.Assets.Sounds;
        int found = 0;
        foreach (var entry in DefaultGameSetContent.Content.Blocks)
        {
            if (entry.Data["Sounds"] is not JsonObject group) continue;
            foreach (var key in new[] { "Working", "Finished" })
            {
                string name = group[key]?.GetValue<string>();
                if (string.IsNullOrEmpty(name)) continue;
                found++;
                Assert.True(sounds.ContainsKey(name), $"{entry.Key}: {key} sound '{name}' is missing");
            }
        }
        Assert.True(found > 0);
    }

    [Theory]
    [InlineData("furnace", "furnaceLoop", "machineDone")]
    [InlineData("crusher", "crusherLoop", "machineDone")]
    [InlineData("disassembler", "disassemblerLoop", "disassemblerDone")]
    [InlineData("cobalt_power_block", "generatorHum", null)]
    public void MachinesHaveSounds(string id, string working, string finished)
    {
        var group = DefaultGameSetContent.Content.FindBlock(id).Data["Sounds"] as JsonObject;
        Assert.NotNull(group);
        Assert.Equal(working, group["Working"]?.GetValue<string>());
        Assert.Equal(finished, group["Finished"]?.GetValue<string>());
    }

    [Theory]
    [InlineData(DrillItem.DefaultSound)]
    [InlineData(EquipSoundNames.Armor)]
    [InlineData(EquipSoundNames.Device)]
    public void BaseGameSetContainsPlayerSound(string name)
    {
        Assert.True(DefaultGameSetContent.Assets.Sounds.ContainsKey(name), name);
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(DefaultGameSetContent.EmptyFolder, "Sounds"), name + ".wav", SearchOption.AllDirectories));
    }
}
