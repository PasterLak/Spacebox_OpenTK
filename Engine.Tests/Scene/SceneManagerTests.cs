using Engine.SceneManagement;

namespace Engine.Tests.Scene;

[Collection(GlobalStateCollection.Name)]
public class SceneManagerTests
{
    private static readonly List<string> Log = new List<string>();

    private abstract class LoggingScene : SceneManagement.Scene
    {
        public override void LoadContent() => Log.Add("load:" + GetType().Name);
        public override void UnloadContent() => Log.Add("unload:" + GetType().Name);

        public override void Start()
        {
            base.Start();
            Log.Add("start:" + GetType().Name);
        }

        public override void Destroy()
        {
            Log.Add("destroy:" + GetType().Name);
            base.Destroy();
        }
    }

    private sealed class FirstScene : LoggingScene { }
    private sealed class SecondScene : LoggingScene { }
    private sealed class UnregisteredScene : LoggingScene { }

    private sealed class ArgsScene : LoggingScene, ISceneWithArgs<int>
    {
        public int Value { get; private set; }

        public void Initialize(int param)
        {
            Value = param;
            Log.Add("init:" + param);
        }
    }

    private struct SceneTestEvent : IEvent { }

    private sealed class CountingListener : IEventListener<SceneTestEvent>
    {
        public int Calls;
        public void OnEvent(ref SceneTestEvent eventData) => Calls++;
    }

    private static void Prepare()
    {
        SceneManager.Register<FirstScene>();
        SceneManager.Register<SecondScene>();
        SceneManager.Register<ArgsScene>();
        SceneManager.Register<ErrorScene>();
        SceneManager.ApplyPendingSceneChange();
        Log.Clear();
    }

    private static void Switch()
    {
        SceneManager.ApplyPendingSceneChange();
    }

    [Fact]
    public void Load_IsDeferredUntilApplied()
    {
        Prepare();
        SceneManager.Load<FirstScene>();

        Assert.Empty(Log);

        Switch();

        Assert.IsType<FirstScene>(SceneManager.Current);
        Assert.Equal("FirstScene", SceneManager.Current.Name);
        Assert.Equal(new[] { "load:FirstScene", "start:FirstScene" }, Log.Where(l => l.EndsWith(":FirstScene")).TakeLast(2));
    }

    [Fact]
    public void Switching_DestroysAndUnloadsPreviousSceneBeforeLoadingNext()
    {
        Prepare();
        SceneManager.Load<FirstScene>();
        Switch();
        Log.Clear();

        SceneManager.Load<SecondScene>();
        Switch();

        Assert.Equal(new[] { "destroy:FirstScene", "unload:FirstScene", "load:SecondScene", "start:SecondScene" }, Log);
        Assert.IsType<SecondScene>(SceneManager.Current);
    }

    [Fact]
    public void LoadWithArgs_InitializesBeforeLoadContent()
    {
        Prepare();
        SceneManager.Load<ArgsScene, int>(42);
        Switch();

        var scene = Assert.IsType<ArgsScene>(SceneManager.Current);
        Assert.Equal(42, scene.Value);
        Assert.True(Log.IndexOf("init:42") < Log.IndexOf("load:ArgsScene"));
    }

    [Fact]
    public void LoadPrevious_ReturnsToEarlierSceneWithNewInstance()
    {
        Prepare();
        SceneManager.Load<FirstScene>();
        Switch();
        var first = SceneManager.Current;
        SceneManager.Load<SecondScene>();
        Switch();

        SceneManager.LoadPrevious();
        Switch();

        Assert.IsType<FirstScene>(SceneManager.Current);
        Assert.NotSame(first, SceneManager.Current);
    }

    [Fact]
    public void Reload_CreatesFreshInstanceOfCurrentScene()
    {
        Prepare();
        SceneManager.Load<FirstScene>();
        Switch();
        SceneManager.Load<SecondScene>();
        Switch();
        var current = SceneManager.Current;
        Log.Clear();

        SceneManager.Reload();
        Switch();

        Assert.IsType<SecondScene>(SceneManager.Current);
        Assert.NotSame(current, SceneManager.Current);
        Assert.Equal(new[] { "destroy:SecondScene", "unload:SecondScene", "load:SecondScene", "start:SecondScene" }, Log);
    }

    [Fact]
    public void LoadUnregistered_FallsBackToErrorScene()
    {
        Prepare();
        SceneManager.Load<UnregisteredScene>();
        Switch();

        Assert.IsType<ErrorScene>(SceneManager.Current);
        Assert.DoesNotContain("load:UnregisteredScene", Log);
    }

    [Fact]
    public void LoadByType_UsesRegisteredScene()
    {
        Prepare();
        SceneManager.Load(typeof(SecondScene));
        Switch();

        Assert.IsType<SecondScene>(SceneManager.Current);
    }

    [Fact]
    public void Switching_ClearsEventBusAndMainCamera()
    {
        Prepare();
        SceneManager.Load<FirstScene>();
        Switch();
        var listener = new CountingListener();
        EventBus.Subscribe(listener);

        SceneManager.Load<SecondScene>();
        Switch();

        var evt = new SceneTestEvent();
        EventBus.Publish(ref evt);
        Assert.Equal(0, listener.Calls);
        Assert.Null(Camera.Main);
    }
}
