using System.Text.Json;
using Spacebox.Game.Mobs;

namespace Spacebox.Tests.Game;

public class MobSoundsTests
{
    [Fact]
    public void IdleDelay_StaysInsideTheConfiguredRange()
    {
        var sounds = new MobSoundsJSON { IdleIntervalMin = 4f, IdleIntervalMax = 9f };
        var random = new Random(3);

        for (int i = 0; i < 200; i++)
            Assert.InRange(MobSounds.NextIdleDelay(sounds, random), 4f, 9f);
    }

    [Fact]
    public void IdleDelay_HandlesSwappedAndTinyValues()
    {
        var sounds = new MobSoundsJSON { IdleIntervalMin = 10f, IdleIntervalMax = 0f };
        Assert.InRange(MobSounds.NextIdleDelay(sounds, new Random(1)), 0.5f, 10f);
    }

    [Fact]
    public void HearingRange_EndsAtSixteenBlocks()
    {
        var mob = new OpenTK.Mathematics.Vector3(100, 0, 0);

        Assert.True(MobSounds.IsInHearingRange(mob, new OpenTK.Mathematics.Vector3(85, 0, 0)));
        Assert.False(MobSounds.IsInHearingRange(mob, new OpenTK.Mathematics.Vector3(83, 0, 0)));
        Assert.False(MobSounds.IsInHearingRange(mob, null));
    }

    [Fact]
    public void MobJsonWithoutSounds_GetsEmptyDefaults()
    {
        var json = JsonSerializer.Deserialize<MobJSON>("{\"Id\":\"x\"}");
        Assert.NotNull(json!.Sounds);
        Assert.Equal("", json.Sounds.Idle);
    }

    [Fact]
    public void DefaultMobSounds_PointToExistingFiles()
    {
        string gameSet = GameSetFolder();
        var soundNames = new HashSet<string>(
            Directory.GetFiles(Path.Combine(gameSet, "Sounds"), "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension),
            StringComparer.Ordinal);

        int checkedSounds = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(gameSet, "Mobs"), "*.json"))
        {
            var mob = JsonSerializer.Deserialize<MobJSON>(File.ReadAllText(file), new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
            foreach (var name in new[] { mob.Sounds.Idle, mob.Sounds.Alert, mob.Sounds.Bite, mob.Sounds.Hurt, mob.Sounds.Death, mob.Sounds.Eat })
            {
                if (string.IsNullOrEmpty(name)) continue;
                Assert.True(soundNames.Contains(name), $"{Path.GetFileName(file)}: sound '{name}' has no file");
                checkedSounds++;
            }
        }

        Assert.True(checkedSounds >= 6);
    }

    private static string GameSetFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Spacebox", "GameSets", "Default"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "Spacebox", "GameSets", "Default");
    }
}
