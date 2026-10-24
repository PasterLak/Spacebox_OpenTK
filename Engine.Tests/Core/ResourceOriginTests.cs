using System.Diagnostics;

namespace Engine.Tests.Core;

public class ResourceOriginTests
{
    [Fact]
    public void Describe_NamesTheCallingMethod()
    {
        string origin = ResourceOrigin.Describe(new StackTrace(false));

        Assert.Contains(nameof(ResourceOriginTests) + "." + nameof(Describe_NamesTheCallingMethod), origin);
    }

    [Fact]
    public void Infrastructure_IsSkipped()
    {
        Assert.True(ResourceOrigin.IsEngineInfrastructure(typeof(Resources)));
        Assert.True(ResourceOrigin.IsEngineInfrastructure(typeof(string)));
        Assert.False(ResourceOrigin.IsEngineInfrastructure(typeof(ResourceOriginTests)));
    }
}
