using OpenTK.Mathematics;
using Spacebox.Game.Generation.Tools;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Game.ShipViewer;

namespace Spacebox.Tests.Ships;

public sealed class ShipPlacementTests
{
    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.True((expected - actual).Length < 1e-4f, $"Expected {expected}, got {actual}");
    }

    [Fact]
    public void AlignedShips_GetThePlayersUpAndFacing()
    {
        var right = new Vector3(0f, 0f, 1f);
        var up = new Vector3(1f, 0f, 0f);
        var front = new Vector3(0f, -1f, 0f);

        var rotation = ShipPlacementRotation.AlignedTo(right, up, front);

        AssertClose(up, Vector3.Transform(Vector3.UnitY, rotation));
        AssertClose(right, Vector3.Transform(Vector3.UnitX, rotation));
        AssertClose(front, Vector3.Transform(-Vector3.UnitZ, rotation));
    }

    [Fact]
    public void Turns_UseTheShipsOwnAxes()
    {
        var aligned = ShipPlacementRotation.AlignedTo(Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitY);

        var turned = ShipPlacementRotation.Turn(aligned, Vector3.UnitY, 90f);

        AssertClose(Vector3.UnitZ, Vector3.Transform(Vector3.UnitY, turned));
        AssertClose(Vector3.Transform(Vector3.UnitX, aligned), Vector3.Transform(Vector3.UnitZ, turned));
    }

    [Fact]
    public void ShipsStart_WithTheirUpAsPlayerUp_AndNoseTowardThePlayer()
    {
        var shipUp = Vector3.UnitZ;
        var shipForward = -Vector3.UnitX;
        var frame = ShipPlacementRotation.FromViewFrame(ShipViewOrientation.FromPilotFrame(shipForward, shipUp));
        var right = new Vector3(0f, 0f, 1f);
        var up = new Vector3(1f, 0f, 0f);
        var front = new Vector3(0f, -1f, 0f);

        var rotation = ShipPlacementRotation.AlignedTo(right, up, front, frame);

        AssertClose(up, Vector3.Transform(shipUp, rotation));
        AssertClose(-front, Vector3.Transform(shipForward, rotation));
    }

    [Fact]
    public void Turns_FollowTheShipsOwnUp()
    {
        var shipUp = Vector3.UnitZ;
        var frame = ShipPlacementRotation.FromViewFrame(ShipViewOrientation.FromPilotFrame(Vector3.UnitX, shipUp));
        var rotation = ShipPlacementRotation.AlignedTo(Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ, frame);

        var turned = ShipPlacementRotation.Turn(rotation, frame, Vector3.UnitY, 90f);

        AssertClose(Vector3.UnitY, Vector3.Transform(shipUp, turned));
    }

    [Fact]
    public void PreviewMesh_HidesFacesBetweenBlocks()
    {
        var cells = new[] { new ShipPreviewCell(new Vector3i(0, 0, 0), 0), new ShipPreviewCell(new Vector3i(1, 0, 0), 0) };

        var (vertices, indices) = ShipPreviewMesh.Build(cells, (_, _) => CubeMeshData.GetBasicUVs());

        Assert.Equal(10 * 6, indices.Length);
        Assert.Equal(10 * 4 * ShipPreviewMesh.FloatsPerVertex, vertices.Length);
    }

    [Fact]
    public void BigShips_AreMergedIntoBiggerCells()
    {
        int scale = ShipPreviewMesh.ScaleFor(ShipPreviewMesh.MaxCells * 10);
        var cells = new[] { new ShipPreviewCell(new Vector3i(0, 0, 0), 1), new ShipPreviewCell(new Vector3i(1, 1, 1), 2), new ShipPreviewCell(new Vector3i(-1, 0, 0), 3) };

        var merged = ShipPreviewMesh.Downscale(cells, 2);

        Assert.True(scale >= 3);
        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, c => c.Cell == new Vector3i(-1, 0, 0));
    }
}
