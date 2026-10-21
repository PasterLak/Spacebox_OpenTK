using Spacebox.Game.Player.Footsteps;

namespace Spacebox.Tests;

public class FootstepCadenceTests
{
    private static int CountSteps(FootstepCadence cadence, float speed, float seconds, FootstepGait gait)
    {
        int steps = 0;
        for (float t = 0f; t < seconds; t += 0.01f)
        {
            if (cadence.Advance(speed, 0.01f, gait)) steps++;
        }
        return steps;
    }

    [Fact]
    public void StandingStillMakesNoSteps()
    {
        Assert.Equal(0, CountSteps(new FootstepCadence(), 0f, 2f, FootstepGait.Walk));
    }

    [Fact]
    public void FirstStepComesSoonAfterStarting()
    {
        var cadence = new FootstepCadence();
        Assert.Equal(1, CountSteps(cadence, 4.5f, 0.1f, FootstepGait.Walk));
    }

    [Fact]
    public void RunningStepsFasterThanWalkingAndCrouching()
    {
        int crouch = CountSteps(new FootstepCadence(), 2.2f, 10f, FootstepGait.Crouch);
        int walk = CountSteps(new FootstepCadence(), 4.5f, 10f, FootstepGait.Walk);
        int run = CountSteps(new FootstepCadence(), 7f, 10f, FootstepGait.Run);

        Assert.InRange(crouch, 14, 18);
        Assert.InRange(walk, 21, 25);
        Assert.InRange(run, 28, 31);
    }

    [Fact]
    public void NaturalBlocksSoundLikeStone()
    {
        Assert.Same(FootstepSurfaces.Stone, FootstepSurfaces.Resolve(null, true));
        Assert.Same(FootstepSurfaces.Metal, FootstepSurfaces.Resolve(Array.Empty<string>(), false));
    }

    [Fact]
    public void BlockFootstepsOverrideSurface()
    {
        var glass = new[] { "footstepGlass1", "footstepGlass2" };

        Assert.Same(glass, FootstepSurfaces.Resolve(glass, true));
        Assert.Same(glass, FootstepSurfaces.Resolve(glass, false));
    }

    [Fact]
    public void MissingFootstepSoundsAreSkipped()
    {
        var missing = new List<string>();
        var names = FootstepSoundNames.KeepExisting(new[] { "footstepGlass1", "", "nope" }, name => name == "footstepGlass1", missing.Add);

        Assert.Equal(new[] { "footstepGlass1" }, names);
        Assert.Equal(new[] { "nope" }, missing);
        Assert.Empty(FootstepSoundNames.KeepExisting(null, _ => true, null));
    }
}
