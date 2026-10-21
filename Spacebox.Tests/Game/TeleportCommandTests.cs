using OpenTK.Mathematics;
using Spacebox.Game.Commands.Teleport;

namespace Spacebox.Tests.Game;

public class TeleportCommandTests
{
    private sealed class FakePlayer : ITeleportable
    {
        public FakePlayer(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }

        public string Name { get; }
        public Vector3 Position { get; private set; }
        public Vector3 Front => -Vector3.UnitZ;

        public void Teleport(Vector3 position) => Position = position;
    }

    [Theory]
    [InlineData("10", 10f, false)]
    [InlineData("-5.5", -5.5f, false)]
    [InlineData("~", 0f, true)]
    [InlineData("~3", 3f, true)]
    [InlineData("~-2.5", -2.5f, true)]
    public void Coordinate_ParsesAbsoluteAndRelative(string text, float value, bool relative)
    {
        Assert.True(TeleportCoordinate.TryParse(text, out var coordinate));
        Assert.Equal(value, coordinate.Value);
        Assert.Equal(relative, coordinate.Relative);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Anna")]
    [InlineData("~x")]
    [InlineData("NaN")]
    public void Coordinate_RejectsInvalidText(string text)
    {
        Assert.False(TeleportCoordinate.TryParse(text, out _));
    }

    [Fact]
    public void Coordinate_RelativeAddsToOrigin()
    {
        Assert.Equal(15f, new TeleportCoordinate(5f, true).Resolve(10f));
        Assert.Equal(5f, new TeleportCoordinate(5f, false).Resolve(10f));
    }

    [Fact]
    public void Request_ThreeCoordinatesTeleportsSelf()
    {
        Assert.True(TeleportRequest.TryParse(new[] { "1", "~2", "3" }, out var request));
        Assert.True(request.HasCoordinates);
        Assert.Null(request.Subjects);
        Assert.Equal(new Vector3(1f, 12f, 3f), request.ResolveCoordinates(new Vector3(100f, 10f, 100f)));
    }

    [Fact]
    public void Request_PlayerAndCoordinates()
    {
        Assert.True(TeleportRequest.TryParse(new[] { "Anna", "1", "2", "3" }, out var request));
        Assert.True(request.HasCoordinates);
        Assert.Equal("Anna", request.Subjects);
    }

    [Fact]
    public void Request_OneNameTeleportsSelfToTarget()
    {
        Assert.True(TeleportRequest.TryParse(new[] { "Anna" }, out var request));
        Assert.False(request.HasCoordinates);
        Assert.Null(request.Subjects);
        Assert.Equal("Anna", request.Destination);
    }

    [Fact]
    public void Request_TwoNamesTeleportsPlayerToTarget()
    {
        Assert.True(TeleportRequest.TryParse(new[] { "Bob", "Anna" }, out var request));
        Assert.Equal("Bob", request.Subjects);
        Assert.Equal("Anna", request.Destination);
    }

    [Theory]
    [InlineData()]
    [InlineData("1", "2", "x")]
    [InlineData("Anna", "1", "2", "x")]
    [InlineData("a", "b", "c", "d", "e")]
    public void Request_RejectsWrongArguments(params string[] args)
    {
        Assert.False(TeleportRequest.TryParse(args, out _));
    }

    [Fact]
    public void Selector_ResolvesSelfAllAndNames()
    {
        var me = new FakePlayer("Me", Vector3.Zero);
        var anna = new FakePlayer("Anna", Vector3.One);
        var players = new List<ITeleportable> { me, anna };
        var results = new List<ITeleportable>();

        Assert.True(TeleportSelector.TrySelect("@s", me, players, results));
        Assert.Equal(new ITeleportable[] { me }, results);

        results.Clear();
        Assert.True(TeleportSelector.TrySelect("@a", me, players, results));
        Assert.Equal(2, results.Count);

        results.Clear();
        Assert.True(TeleportSelector.TrySelect("anna", me, players, results));
        Assert.Same(anna, results[0]);

        results.Clear();
        Assert.False(TeleportSelector.TrySelect("Bob", me, players, results));
        Assert.False(TeleportSelector.TrySelect("@s", null, players, results));
    }
}
