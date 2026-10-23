using Engine.Components;
using Engine.Graphics.Occlusion;
using OpenTK.Mathematics;

namespace Engine.Tests.Physics;

[Collection(GlobalStateCollection.Name)]
public class BVHCullerTests
{
    private sealed class RenderCounter : Component
    {
        public int Renders;

        public override void OnRender() => Renders++;
    }

    private sealed class BoxNode : Node3D, IRenderBounds
    {
        private readonly Vector3 _min;
        private readonly Vector3 _max;

        public BoxNode(Vector3 center, float halfSize)
        {
            _min = center - new Vector3(halfSize);
            _max = center + new Vector3(halfSize);
        }

        public bool TryGetRenderBounds(out Vector3 min, out Vector3 max)
        {
            min = _min;
            max = _max;
            return true;
        }
    }

    private static (Node3D Root, CameraStatic Camera) CreateScene()
    {
        var previous = Camera.Main;
        var camera = new CameraStatic(Vector3.Zero);
        Camera.Main = previous;
        camera.Frustum.SetFromMatrices(
            Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY),
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), 1f, 0.1f, 1000f));
        var root = new Node3D();
        root.AddChild(camera);
        return (root, camera);
    }

    private static RenderCounter AddBox(Node3D parent, Vector3 center)
    {
        var node = parent.AddChild(new BoxNode(center, 1f));
        return node.AttachComponent(new RenderCounter());
    }

    [Fact]
    public void Render_DrawsVisibleNodesAndCullsOthers()
    {
        var (root, camera) = CreateScene();
        var visible = AddBox(root, new Vector3(0f, 0f, -10f));
        var behind = AddBox(root, new Vector3(0f, 0f, 10f));

        BVHCuller.Render(root, camera);

        Assert.Equal(1, visible.Renders);
        Assert.Equal(0, behind.Renders);
        Assert.Equal(1, BVHCuller.VisibleObjects);
        Assert.Equal(1, BVHCuller.CulledObjects);
    }

    [Fact]
    public void Render_ChildrenOfCulledNodeAreStillTested()
    {
        var (root, camera) = CreateScene();
        var parent = root.AddChild(new BoxNode(new Vector3(0f, 0f, 10f), 1f));
        var child = AddBox(parent, new Vector3(0f, 0f, -10f));

        BVHCuller.Render(root, camera);

        Assert.Equal(1, child.Renders);
    }

    [Fact]
    public void Render_NodesUnderTheCameraAreNeverCulled()
    {
        var (root, camera) = CreateScene();
        var attached = AddBox(camera, new Vector3(0f, 0f, 10f));

        BVHCuller.Render(root, camera);

        Assert.Equal(1, attached.Renders);
        Assert.Equal(0, BVHCuller.CulledObjects);
    }

    [Fact]
    public void Render_SkipsDisabledSubtrees()
    {
        var (root, camera) = CreateScene();
        var group = root.AddChild(new Node3D());
        var hidden = AddBox(group, new Vector3(0f, 0f, -10f));
        group.Enabled = false;

        BVHCuller.Render(root, camera);

        Assert.Equal(0, hidden.Renders);
    }
}
