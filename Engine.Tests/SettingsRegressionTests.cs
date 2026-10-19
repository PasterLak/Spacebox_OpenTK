using Engine.InputPro;
using Engine.PostProcessing;
using OpenTK.Mathematics;

namespace Engine.Tests;

public class SettingsRegressionTests
{
    private sealed class TestEffect : PostProcessEffect
    {
        private readonly bool _required;

        public TestEffect(bool required)
        {
            _required = required;
        }

        public override bool IsRequired => _required;

        public override void Apply(int inputTexture, int outputFbo, Vector2i clientSize)
        {
        }
    }

    [Fact]
    public void PostProcessingOff_KeepsRequiredEffects()
    {
        Assert.True(new TestEffect(true).ShouldApply(false));
        Assert.False(new TestEffect(false).ShouldApply(false));
        Assert.True(new TestEffect(false).ShouldApply(true));
        Assert.False(new TestEffect(true) { Enabled = false }.ShouldApply(true));
    }

    [Fact]
    public void ReloadedProfile_KeepsActionObjectsAndSubscriptions()
    {
        var previous = new InputProfile();
        var flashlight = new InputAction("Flashlight");
        int calls = 0;
        flashlight.Subscribe(InputEventType.Pressed, () => calls++);
        previous.Actions["flashlight"] = flashlight;
        previous.Actions["extra"] = new InputAction("Extra");

        var loaded = new InputProfile();
        var reloaded = new InputAction("Flashlight Renamed", "new description");
        loaded.Actions["flashlight"] = reloaded;
        loaded.ActionOrder.Add(reloaded);

        loaded.KeepExistingActions(previous);

        Assert.Same(flashlight, loaded.Actions["flashlight"]);
        Assert.Same(flashlight, loaded.ActionOrder[0]);
        Assert.Equal("Flashlight Renamed", flashlight.Name);
        Assert.Equal("new description", flashlight.Description);
        Assert.True(loaded.Actions.ContainsKey("extra"));
    }

    [Fact]
    public void ParticleSwitch_IsOnByDefault()
    {
        Assert.True(ParticleSystem.GloballyEnabled);
    }
}
