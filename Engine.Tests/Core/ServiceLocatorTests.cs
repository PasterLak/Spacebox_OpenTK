namespace Engine.Tests.Core;

[Collection(GlobalStateCollection.Name)]
public class ServiceLocatorTests
{
    private interface IAudioServiceStub { }
    private interface IMissingServiceStub { }
    private interface IDuplicateServiceStub { }
    private sealed class AudioServiceStub : IAudioServiceStub, IDuplicateServiceStub { }

    [Fact]
    public void Register_ThenGet_ReturnsService()
    {
        var service = new AudioServiceStub();
        ServiceLocator.Register<IAudioServiceStub>(service);
        try
        {
            Assert.Same(service, ServiceLocator.Get<IAudioServiceStub>());
            Assert.True(ServiceLocator.TryGet<IAudioServiceStub>(out var found));
            Assert.Same(service, found);
        }
        finally
        {
            ServiceLocator.Unregister<IAudioServiceStub>();
        }

        Assert.False(ServiceLocator.TryGet<IAudioServiceStub>(out _));
    }

    [Fact]
    public void Get_Missing_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => ServiceLocator.Get<IMissingServiceStub>());
        Assert.False(ServiceLocator.TryGet<IMissingServiceStub>(out _));
    }

    [Fact]
    public void Register_Twice_Throws()
    {
        ServiceLocator.Register<IDuplicateServiceStub>(new AudioServiceStub());
        try
        {
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Register<IDuplicateServiceStub>(new AudioServiceStub()));
        }
        finally
        {
            ServiceLocator.Unregister<IDuplicateServiceStub>();
        }
    }

    [Fact]
    public void Register_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<IMissingServiceStub>(null!));
    }
}
