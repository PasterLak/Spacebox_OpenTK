using OpenTK.Mathematics;

namespace Engine.Tests;

public class LineRendererTests
{
    [Fact]
    public void ChooseUp_UsesTheReferenceUnlessParallel()
    {
        var shooterUp = new Vector3(0, 0.6f, 0.8f);

        Assert.Equal(shooterUp, LineRenderer.ChooseUp(Vector3.UnitX, shooterUp));
        Assert.Equal(Vector3.UnitY, LineRenderer.ChooseUp(Vector3.UnitZ, Vector3.UnitZ));
        Assert.Equal(Vector3.UnitZ, LineRenderer.ChooseUp(Vector3.UnitY, Vector3.UnitY));
    }
}
