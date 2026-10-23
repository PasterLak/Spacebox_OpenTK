using Engine.Light;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics;

[Collection(GlobalStateCollection.Name)]
public sealed class LightingTests
{
    [Fact]
    public void ResetToDefaults_UndoesSceneLighting()
    {
        Lighting.AmbientColor = new Vector3(0.2f);
        Lighting.FogColor = new Vector3(0.5f);
        Lighting.FogDensity = 70;

        Lighting.ResetToDefaults();

        Assert.Equal(Lighting.DefaultAmbientColor, Lighting.AmbientColor);
        Assert.Equal(Vector3.Zero, Lighting.FogColor);
        Assert.Equal(Lighting.DefaultFogDensity, Lighting.FogDensity);
    }
}
