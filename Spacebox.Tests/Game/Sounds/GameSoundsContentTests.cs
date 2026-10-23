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
            Assert.True(ClipExists(spec.Clip), spec.Clip);
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
    [InlineData("furnace", "furnaceLoop", null)]
    [InlineData("crusher", "crusherLoop", null)]
    [InlineData("disassembler", "disassemblerLoop", "disassemblerDone")]
    [InlineData("cobalt_power_block", "generatorHum", null)]
    public void MachinesHaveSounds(string id, string working, string finished)
    {
        var group = DefaultGameSetContent.Content.FindBlock(id).Data["Sounds"] as JsonObject;
        Assert.NotNull(group);
        Assert.Equal(working, group["Working"]?.GetValue<string>());
        Assert.Equal(finished, group["Finished"]?.GetValue<string>());
    }

    private static bool ClipExists(string clip)
    {
        if (clip.Contains('/')) return Directory.GetFiles(Path.Combine(SpaceboxFolder, Path.GetDirectoryName(clip)!), Path.GetFileName(clip) + ".*").Length > 0;
        return DefaultGameSetContent.Assets.Sounds.ContainsKey(clip) || Directory.GetFiles(Path.Combine(SpaceboxFolder, "Resources", "Audio"), clip + ".*", SearchOption.AllDirectories).Length > 0;
    }
}
