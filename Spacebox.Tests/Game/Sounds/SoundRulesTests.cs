using Spacebox.Game;
using Spacebox.Game.Player;
using Spacebox.Game.Player.Interactions;
using Spacebox.Game.Resource;
using Xunit;

namespace Spacebox.Tests.Game.Sounds;

public class SoundRulesTests
{
    [Theory]
    [InlineData(EquipmentSlotType.Helmet)]
    [InlineData(EquipmentSlotType.Flashlight)]
    public void EquipSoundIsSilentByDefault(EquipmentSlotType slot)
    {
        Assert.Equal("", EquipSoundNames.Resolve(slot, ""));
        Assert.Equal("", EquipSoundNames.Resolve(slot, null));
    }

    [Fact]
    public void ConfiguredEquipSoundWins()
    {
        Assert.Equal("myClank", EquipSoundNames.Resolve(EquipmentSlotType.Chest, " myClank "));
    }

    [Fact]
    public void NewJsonSoundFieldsHaveDefaults()
    {
        var group = new SoundGroup();
        Assert.Equal("", group.Working);
        Assert.Equal("", group.Finished);
        Assert.Equal(DrillItem.DefaultSound, new DrillItemJSON().Sound);
        Assert.Equal("", new EquipmentJSON().EquipSound);
    }

    [Fact]
    public void BlockDataCopiesMachineSounds()
    {
        var data = new BlockData("furnace", "furnace") { WorkingSound = "loop", FinishedSound = "done" };
        var copy = new BlockData(data);

        Assert.Equal("loop", copy.WorkingSound);
        Assert.Equal("done", copy.FinishedSound);
    }

    [Fact]
    public void DrillFadesInGently()
    {
        var mixer = new DrillSoundMixer();
        mixer.DrillBlock(1, true);
        mixer.Update(0.05f);

        Assert.True(mixer.Volume > 0f);
        Assert.True(mixer.Volume < DrillSoundMixer.BaseVolume * 0.5f);

        for (int i = 0; i < 20; i++)
        {
            mixer.DrillBlock(1, true);
            mixer.Update(0.05f);
        }
        Assert.Equal(DrillSoundMixer.BaseVolume, mixer.Volume, 3);
    }

    [Fact]
    public void DrillFadesOutWhenReleased()
    {
        var mixer = new DrillSoundMixer();
        for (int i = 0; i < 20; i++)
        {
            mixer.DrillBlock(2, true);
            mixer.Update(0.05f);
        }

        mixer.Update(0.05f);
        Assert.True(mixer.Volume > 0f);
        Assert.True(mixer.Volume < DrillSoundMixer.BaseVolume);

        for (int i = 0; i < 20; i++) mixer.Update(0.05f);
        Assert.False(mixer.IsAudible);
    }

    [Fact]
    public void HarderBlocksSoundLowerAndBlockedLowest()
    {
        float soft = DrillSoundMixer.PitchForHardness(1);
        float hard = DrillSoundMixer.PitchForHardness(5);

        Assert.True(hard < soft);
        Assert.True(DrillSoundMixer.BlockedPitch < hard);
        Assert.InRange(hard, 0.8f, 1.1f);
        Assert.Equal(DrillSoundMixer.PitchForHardness(5), DrillSoundMixer.PitchForHardness(50));
    }

    [Fact]
    public void LowOxygenBeepsAtOnceThenRepeats()
    {
        var schedule = new LowOxygenBeepSchedule();

        Assert.True(schedule.Update(0.1f, 0.016f));
        Assert.False(schedule.Update(0.1f, 1f));
        Assert.True(schedule.Update(0.1f, LowOxygenBeepSchedule.SlowInterval));
    }

    [Fact]
    public void LowOxygenStopsWhenRestored()
    {
        var schedule = new LowOxygenBeepSchedule();
        schedule.Update(0.5f, 0.016f);

        Assert.False(schedule.Update(0f, 10f));
        Assert.False(schedule.IsActive);
        Assert.True(schedule.Update(0.5f, 0.016f));
    }

    [Fact]
    public void LowOxygenBeepsFasterWhenEmpty()
    {
        Assert.True(LowOxygenBeepSchedule.Interval(1f) < LowOxygenBeepSchedule.Interval(0.1f));
        Assert.Equal(LowOxygenBeepSchedule.FastInterval, LowOxygenBeepSchedule.Interval(1f), 3);
    }
}
