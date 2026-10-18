using Engine.Components;
using Engine.Light;
using OpenTK.Mathematics;

namespace Engine.Tests.Scene;

[Collection(GlobalStateCollection.Name)]
public class SceneTests
{
    private sealed class TestScene : SceneManagement.Scene
    {
        public int Loads;
        public int Unloads;

        public override void LoadContent() => Loads++;
        public override void UnloadContent() => Unloads++;
    }

    private sealed class CountingComponent : Component
    {
        public int Starts;
        public int Updates;
        public bool Detached;

        public override void Start() => Starts++;
        public override void OnUpdate() => Updates++;

        public override void OnDetached()
        {
            Detached = true;
            base.OnDetached();
        }
    }

    [Fact]
    public void NewScene_HasDefaultsAndIgnoresScale()
    {
        var scene = new TestScene();

        Assert.Equal("DefaultName", scene.Name);
        Assert.False(scene.Resizable);

        scene.Scale = new Vector3(5);
        var child = scene.AddChild(new Node3D(new Vector3(1, 0, 0)));

        Assert.Equal(new Vector3(1, 0, 0), child.PositionWorld);
    }

    [Fact]
    public void StartAndUpdate_ReachWholeGraph()
    {
        var scene = new TestScene();
        var component = new CountingComponent();
        scene.AddChild(new Node3D()).AddChild(new Node3D()).AttachComponent(component);

        scene.Start();
        scene.Update();
        scene.Update();

        Assert.Equal(1, component.Starts);
        Assert.Equal(2, component.Updates);
    }

    [Fact]
    public void Destroy_TearsDownGraphAndSkybox()
    {
        var scene = new TestScene();
        var component = new CountingComponent();
        var child = scene.AddChild(new Node3D());
        child.AttachComponent(component);
        var skybox = new Node3D();
        Lighting.Skybox = skybox;
        var skyboxChild = skybox.AddChild(new Node3D());

        scene.Destroy();

        Assert.Empty(scene.Children);
        Assert.Null(child.Parent);
        Assert.True(component.Detached);
        Assert.Null(Lighting.Skybox);
        Assert.Empty(skybox.Children);
        Assert.Null(skyboxChild.Parent);
    }

    [Fact]
    public void ReplacingSkybox_DestroysPreviousOne()
    {
        var first = new Node3D();
        var firstChild = first.AddChild(new Node3D());

        Lighting.Skybox = first;
        Lighting.Skybox = new Node3D();

        Assert.Empty(first.Children);
        Assert.Null(firstChild.Parent);

        Lighting.Skybox = null;
    }
}
