using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Tests.Ships;

public class SpaceEntityPoseTests
{
    private static SpaceEntity CreateEntity(long id = -5)
    {
        return new SpaceEntity(id, new Vector3(100, 50, -20), null!);
    }

    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(Vector3.Distance(expected, actual), 0f, 0.001f);
    }

    [Fact]
    public void DynamicEntities_HaveShipBody()
    {
        Assert.NotNull(CreateEntity(-1).Ship);
        Assert.Null(CreateEntity(7).Ship);
    }

    [Fact]
    public void SetPose_TranslatesEntityAndKeepsLocalCoordinates()
    {
        var entity = CreateEntity();
        var local = new Vector3(3, 4, 5);
        var comBefore = entity.CenterOfMass;

        entity.SetPose(new Vector3(110, 50, -20), Quaternion.Identity);

        AssertClose(new Vector3(113, 54, -15), entity.EntityLocalToWorld(local));
        AssertClose(local, entity.WorldToEntityLocal(new Vector3(113, 54, -15)));
        AssertClose(comBefore + new Vector3(10, 0, 0), entity.CenterOfMass);
        AssertClose(new Vector3(110, 50, -20), entity.BoundingBox.Center);
    }

    [Fact]
    public void SetPose_RotationRoundTrips()
    {
        var entity = CreateEntity();
        var orientation = Quaternion.FromAxisAngle(Vector3.UnitY, 1.1f);
        var local = new Vector3(-2, 7, 1);

        entity.SetPose(new Vector3(0, 0, 0), orientation);

        AssertClose(local, entity.WorldToEntityLocal(entity.EntityLocalToWorld(local)));
        AssertClose(Vector3.Transform(local, orientation), entity.EntityLocalToWorld(local));
        Assert.True(entity.HasRotation);
    }

    [Fact]
    public void ShipBody_DoesNotMoveEntityWithoutBlocks()
    {
        var entity = CreateEntity();
        entity.Ship!.Motion.Velocity = new Vector3(0, 0, 2);
        var before = entity.PositionWorld;

        entity.Ship.Update(0.05f);

        Assert.Equal(before, entity.PositionWorld);
    }
}
