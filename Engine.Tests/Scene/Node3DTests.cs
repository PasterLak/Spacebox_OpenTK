using Engine.Components;
using OpenTK.Mathematics;

namespace Engine.Tests.Scene;

public class Node3DTests
{
    private const float Precision = 0.0001f;

    private sealed class CountingComponent : Component
    {
        public int Starts;
        public int Updates;
        public int Detaches;

        public override void Start() => Starts++;
        public override void OnUpdate() => Updates++;

        public override void OnDetached()
        {
            Detaches++;
            base.OnDetached();
        }
    }

    private sealed class MarkerNode : Node3D { }

    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(Vector3.Distance(expected, actual), 0f, Precision);
    }

    [Fact]
    public void AddChild_SetsParentAndChildren()
    {
        var root = new Node3D();
        var child = root.AddChild(new Node3D());

        Assert.Same(root, child.Parent);
        Assert.Contains(child, root.Children);
        Assert.True(root.IsRoot);
        Assert.False(child.IsRoot);
        Assert.Same(root, child.GetRoot());
    }

    [Fact]
    public void AddChild_Self_IsIgnored()
    {
        var node = new Node3D();

        node.AddChild(node);

        Assert.Null(node.Parent);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void Reparent_RemovesFromOldParent()
    {
        var a = new Node3D();
        var b = new Node3D();
        var child = a.AddChild(new Node3D());

        b.AddChild(child);

        Assert.Empty(a.Children);
        Assert.Single(b.Children);
        Assert.Same(b, child.Parent);
    }

    [Fact]
    public void RemoveChild_ClearsParent()
    {
        var root = new Node3D();
        var child = root.AddChild(new Node3D());

        root.RemoveChild(child);

        Assert.Null(child.Parent);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void PositionWorld_CombinesParentTranslationRotationAndScale()
    {
        var parent = new Node3D(new Vector3(10, 0, 0)) { Rotation = new Vector3(0, 90, 0), Scale = new Vector3(2) };
        var child = parent.AddChild(new Node3D(new Vector3(1, 0, 0)));

        AssertClose(new Vector3(10, 0, -2), child.PositionWorld);
    }

    [Fact]
    public void MovingParent_UpdatesCachedChildMatrix()
    {
        var parent = new Node3D();
        var child = parent.AddChild(new Node3D(new Vector3(0, 1, 0)));
        _ = child.GetModelMatrix();

        parent.Position = new Vector3(5, 0, 0);

        AssertClose(new Vector3(5, 1, 0), child.PositionWorld);
        AssertClose(new Vector3(5, 1, 0), child.LocalToWorld(Vector3.Zero));
    }

    [Fact]
    public void WorldToLocal_IsInverseOfLocalToWorld()
    {
        var parent = new Node3D(new Vector3(3, -2, 7)) { Rotation = new Vector3(30, 45, 10) };
        var node = parent.AddChild(new Node3D(new Vector3(1, 2, 3)) { Scale = new Vector3(1.5f) });
        var point = new Vector3(-4, 8, 0.5f);

        AssertClose(point, node.WorldToLocal(node.LocalToWorld(point)));
    }

    [Fact]
    public void Resizable_False_IgnoresScale()
    {
        var node = new FixedSizeNode { Scale = new Vector3(10) };

        AssertClose(new Vector3(1, 0, 0), node.LocalToWorld(new Vector3(1, 0, 0)));
    }

    private sealed class FixedSizeNode : Node3D
    {
        public FixedSizeNode() => Resizable = false;
    }

    [Fact]
    public void Translate_MovesAlongLocalAxes()
    {
        var node = new Node3D { Rotation = new Vector3(0, 90, 0) };

        node.Translate(new Vector3(0, 0, -1));

        AssertClose(new Vector3(-1, 0, 0), node.Position);
        AssertClose(new Vector3(-1, 0, 0), node.ForwardLocal);
    }

    [Fact]
    public void LookAt_PointsForwardAtTarget()
    {
        var node = new Node3D(new Vector3(1, 1, 1));
        var target = new Vector3(4, 5, 1);

        node.LookAt(target);

        AssertClose((target - node.Position).Normalized(), node.ForwardWorld);
    }

    [Fact]
    public void RotateAround_KeepsDistanceToPivot()
    {
        var node = new Node3D(new Vector3(2, 0, 0));

        node.RotateAround(Vector3.Zero, Vector3.UnitY, 90);

        AssertClose(new Vector3(0, 0, -2), node.Position);
    }

    [Fact]
    public void Find_SearchesWholeTree()
    {
        var root = new Node3D { Name = "root" };
        var a = root.AddChild(new Node3D { Name = "a", Tag = "enemy" });
        var deep = a.AddChild(new MarkerNode { Name = "deep", Tag = "enemy" });

        Assert.Same(deep, root.FindChildByName("deep"));
        Assert.Same(a, root.FindChildByTag("enemy"));
        Assert.Equal(2, root.FindAllChildrenByTag("enemy").Count);
        Assert.Same(deep, root.FindNode<MarkerNode>());
        Assert.Single(root.FindAllNodesOfType<MarkerNode>());
        Assert.Null(root.FindChildByName("missing"));

        int visited = 0;
        root.Traverse(_ => visited++);
        Assert.Equal(3, visited);
    }

    [Fact]
    public void GetChild_OnlyLooksAtDirectChildren()
    {
        var root = new Node3D();
        var a = root.AddChild(new Node3D());
        a.AddChild(new MarkerNode());

        Assert.Null(root.GetChild<MarkerNode>());
        Assert.NotNull(a.GetChild<MarkerNode>());
        Assert.Equal(2, root.GetChildren<Node3D>().Count + a.GetChildren<Node3D>().Count);
    }

    [Fact]
    public void Components_AttachOnceAndKnowOwner()
    {
        var node = new Node3D();
        var component = new CountingComponent();

        node.AttachComponent(component);
        node.AttachComponent(component);

        Assert.Single(node.Components);
        Assert.Same(node, component.Owner);
        Assert.Same(component, node.GetComponent<CountingComponent>());
        Assert.True(node.TryGetComponent<CountingComponent>(out _));

        node.DetachComponent(component);

        Assert.Empty(node.Components);
        Assert.Null(component.Owner);
        Assert.Equal(1, component.Detaches);
    }

    [Fact]
    public void FindComponent_SearchesChildren()
    {
        var root = new Node3D();
        var child = root.AddChild(new Node3D());
        child.AttachComponent(new CountingComponent());
        child.AddChild(new Node3D()).AttachComponent(new CountingComponent());

        Assert.NotNull(root.FindComponent<CountingComponent>());
        Assert.Null(root.GetComponent<CountingComponent>());
        Assert.Equal(2, root.FindAllComponents<CountingComponent>().Count);
    }

    [Fact]
    public void Update_SkipsDisabledNodesAndComponents()
    {
        var root = new Node3D();
        var active = new CountingComponent();
        var disabledComponent = new CountingComponent { Enabled = false };
        var inDisabledChild = new CountingComponent();
        root.AttachComponent(active);
        root.AttachComponent(disabledComponent);
        root.AddChild(new Node3D { Enabled = false }).AttachComponent(inDisabledChild);

        root.Update();

        Assert.Equal(1, active.Updates);
        Assert.Equal(0, disabledComponent.Updates);
        Assert.Equal(0, inDisabledChild.Updates);
    }

    [Fact]
    public void Start_ReachesChildComponents()
    {
        var root = new Node3D();
        var component = new CountingComponent();
        root.AddChild(new Node3D()).AttachComponent(component);

        root.Start();

        Assert.Equal(1, component.Starts);
    }

    [Fact]
    public void Destroy_DetachesFromParentAndClearsChildren()
    {
        var root = new Node3D();
        var node = root.AddChild(new Node3D());
        var grandChild = node.AddChild(new Node3D());
        var component = new CountingComponent();
        grandChild.AttachComponent(component);

        node.Destroy();

        Assert.Empty(root.Children);
        Assert.Empty(node.Children);
        Assert.Null(grandChild.Parent);
        Assert.Equal(1, component.Detaches);
    }

    [Fact]
    public void Enabled_RaisesChangeEvent()
    {
        var node = new Node3D();
        bool? received = null;
        node.OnEnabledChanged = value => received = value;

        node.Enabled = false;

        Assert.False(received);
    }

    [Fact]
    public void Equality_IsByIdentity()
    {
        var a = new Node3D { Name = "same" };
        var b = new Node3D { Name = "same" };

        Assert.True(a == a);
        Assert.False(a == b);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void QuaternionToEuler_RoundTripsThroughOrientation()
    {
        var node = new Node3D { Rotation = new Vector3(20, 40, 60) };

        var euler = Node3D.QuaternionToEuler(node.Orientation);

        AssertClose(new Node3D { Rotation = euler }.ForwardLocal, node.ForwardLocal);
    }
}
