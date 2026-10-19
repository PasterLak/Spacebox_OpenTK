using System.Text.Json;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Player;

namespace Spacebox.Tests;

public class DistanceTrackerTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void FirstStep_OnlyStoresPosition()
    {
        var tracker = new DistanceTracker();

        Assert.Equal(0, tracker.Step(new Vector3(100, 0, 0), Frame));
    }

    [Fact]
    public void SmallFrameSteps_AreNotLost()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);
        double total = 0;

        for (int i = 1; i <= 600; i++)
            total += tracker.Step(new Vector3(i * 5f * Frame, 0, 0), Frame);

        Assert.Equal(50.0, total, 2);
        Assert.Equal(5f, tracker.LastSpeed, 2);
    }

    [Fact]
    public void CurvedPath_CountsPathLengthNotChord()
    {
        var tracker = new DistanceTracker();
        const float radius = 20f;
        tracker.Reset(new Vector3(radius, 0, 0));
        double total = 0;
        int steps = 3600;

        for (int i = 1; i <= steps; i++)
        {
            float angle = MathF.Tau * i / steps;
            total += tracker.Step(new Vector3(MathF.Cos(angle) * radius, 0, MathF.Sin(angle) * radius), Frame);
        }

        Assert.Equal(MathF.Tau * radius, total, 1);
    }

    [Fact]
    public void Jitter_IsIgnored()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(new Vector3(6000, 7000, 6000));
        double total = 0;

        for (int i = 0; i < 1000; i++)
            total += tracker.Step(new Vector3(6000 + (i % 2) * 0.0004f, 7000, 6000), Frame);

        Assert.Equal(0, total);
    }

    [Fact]
    public void ImpossibleJump_IsTreatedAsTeleport()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);

        Assert.Equal(0, tracker.Step(new Vector3(5000, 0, 0), Frame));
        Assert.Equal(0f, tracker.LastSpeed);
        Assert.Equal(1.0, tracker.Step(new Vector3(5001, 0, 0), Frame), 3);
    }

    [Fact]
    public void FastShipSpeed_IsStillCounted()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);

        Assert.Equal(5.0, tracker.Step(new Vector3(5, 0, 0), Frame), 3);
        Assert.Equal(300f, tracker.LastSpeed, 1);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void InvalidDelta_AddsNothing(float delta)
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);

        Assert.Equal(0, tracker.Step(new Vector3(1, 0, 0), delta));
        Assert.Equal(1.0, tracker.Step(new Vector3(2, 0, 0), Frame), 3);
    }

    [Fact]
    public void NaNPosition_IsIgnoredAndRestarts()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);

        Assert.Equal(0, tracker.Step(new Vector3(float.NaN, 0, 0), Frame));
        Assert.Equal(0, tracker.Step(new Vector3(1, 0, 0), Frame));
        Assert.Equal(1.0, tracker.Step(new Vector3(2, 0, 0), Frame), 3);
    }

    [Fact]
    public void Invalidate_SkipsMovementWhileNotTracked()
    {
        var tracker = new DistanceTracker();
        tracker.Reset(Vector3.Zero);
        tracker.Invalidate();

        Assert.Equal(0, tracker.Step(new Vector3(3, 0, 0), Frame));
        Assert.Equal(1.0, tracker.Step(new Vector3(4, 0, 0), Frame), 3);
    }
}

public class PlayerStatisticsTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Distance_AddsValidValuesAndShipsSeparately()
    {
        var stats = new PlayerStatistics();

        stats.AddDistance(10.5, false);
        stats.AddDistance(4.5, true);
        stats.AddDistance(-3, false);
        stats.AddDistance(double.NaN, false);
        stats.AddDistance(double.PositiveInfinity, true);

        Assert.Equal(15.0, stats.DistanceTraveled, 6);
        Assert.Equal(4.5, stats.DistanceInShips, 6);
    }

    [Fact]
    public void Speed_KeepsMaximum()
    {
        var stats = new PlayerStatistics();

        stats.RecordSpeed(12f);
        stats.RecordSpeed(7f);
        stats.RecordSpeed(float.NaN);

        Assert.Equal(12f, stats.MaxSpeedReached);
    }

    [Fact]
    public void Session_CountsOncePerLoadNotPerSave()
    {
        var stats = new PlayerStatistics();
        stats.StartSession(Now);

        stats.PrepareForSave(Now);
        stats.PrepareForSave(Now);
        stats.PrepareForSave(Now);

        Assert.Equal(1, stats.SessionsPlayed);
        Assert.Equal(Now, stats.FirstPlayedUtc);
    }

    [Fact]
    public void PlayTime_AccumulatesFractions()
    {
        var stats = new PlayerStatistics();
        stats.StartSession(Now);

        for (int i = 0; i < 90 * 60; i++)
            stats.AddPlayTime(1f / 60f);
        stats.PrepareForSave(Now);

        Assert.Equal(90.0, stats.PlayTimeSeconds, 1);
        Assert.Equal(1, stats.TotalPlayTimeMinutes);
        Assert.Equal(1, stats.SessionMinutes);
    }

    [Fact]
    public void PlayTime_IgnoresInvalidValues()
    {
        var stats = new PlayerStatistics();

        stats.AddPlayTime(-5);
        stats.AddPlayTime(double.NaN);

        Assert.Equal(0, stats.PlayTimeSeconds);
    }

    [Fact]
    public void LegacyMinutes_AreMigratedToSeconds()
    {
        var stats = new PlayerStatistics { TotalPlayTimeMinutes = 30 };

        stats.StartSession(Now);

        Assert.Equal(1800.0, stats.PlayTimeSeconds);
        Assert.Equal(30, stats.TotalPlayTimeMinutes);
    }

    [Fact]
    public void FirstPlayed_IsKeptOnLaterSessions()
    {
        var first = Now.AddDays(-3);
        var stats = new PlayerStatistics { FirstPlayedUtc = first };

        stats.StartSession(Now);

        Assert.Equal(first, stats.FirstPlayedUtc);
        Assert.Equal(Now, stats.LastPlayedUtc);
    }

    [Fact]
    public void Deaths_CountCausesWithUnknownFallback()
    {
        var stats = new PlayerStatistics();

        stats.RecordDeath("Fell");
        stats.RecordDeath("Fell");
        stats.RecordDeath(null);
        stats.RecordDeath("  ");

        Assert.Equal(4, stats.DeathsTotal);
        Assert.Equal(2, stats.DeathCauses["Fell"]);
        Assert.Equal(2, stats.DeathCauses[PlayerStatistics.UnknownDeathCause]);
    }

    [Fact]
    public void TrackedKeys_AreCapped()
    {
        var stats = new PlayerStatistics();

        for (int i = 0; i < PlayerStatistics.MaxTrackedKeys + 10; i++)
            stats.RecordItemUse("item" + i);

        Assert.Equal(PlayerStatistics.MaxTrackedKeys + 1, stats.ItemsUsed.Count);
        Assert.Equal(10, stats.ItemsUsed[PlayerStatistics.OtherKey]);
    }

    [Fact]
    public void Exploration_CountsUniqueAsteroidsAndSectors()
    {
        var stats = new PlayerStatistics();

        Assert.True(stats.RecordAsteroid(5));
        Assert.False(stats.RecordAsteroid(5));
        stats.RecordAsteroid(6);
        stats.RecordSector("0,0,0");
        stats.RecordSector("0,0,0");
        stats.RecordSector("1,0,0");
        Assert.False(stats.RecordSector(""));

        Assert.Equal(2, stats.AsteroidsDiscovered);
        Assert.Equal(2, stats.SectorsExplored);
    }

    [Fact]
    public void Damage_IgnoresInvalidValues()
    {
        var stats = new PlayerStatistics();

        stats.RecordDamageTaken(5);
        stats.RecordDamageTaken(-3);
        stats.RecordEntityDamage(2.5);
        stats.RecordEntityDamage(double.NaN);
        stats.RecordEntityDamage(-1);

        Assert.Equal(5, stats.DamageTaken);
        Assert.Equal(2.5, stats.EntityDamageDealt);
    }

    [Fact]
    public void Accuracy_IsClampedAndSafe()
    {
        Assert.Equal(0f, new PlayerStatistics().GetAccuracy());
        Assert.Equal(50f, new PlayerStatistics { ShotsFired = 4, ShotsHit = 2 }.GetAccuracy());
        Assert.Equal(100f, new PlayerStatistics { ShotsFired = 1, ShotsHit = 3 }.GetAccuracy());
    }

    [Fact]
    public void AverageSession_UsesSeconds()
    {
        var stats = new PlayerStatistics { PlayTimeSeconds = 3 * 600, SessionsPlayed = 3 };

        Assert.Equal(10, stats.GetAverageSessionTimeMinutes());
    }

    [Fact]
    public void ProcessedItems_OnlyPositive()
    {
        var stats = new PlayerStatistics();

        stats.AddProcessedItems(4);
        stats.AddProcessedItems(-2);

        Assert.Equal(4, stats.ItemsProcessed);
    }

    [Fact]
    public void CountMovedFrom_HandlesPartialMovesAndSwaps()
    {
        var iron = new Item(1, 64, "Iron");
        var copper = new Item(2, 64, "Copper");

        Assert.Equal(10, PlayerStatistics.CountMovedFrom(iron, 10, null, 0));
        Assert.Equal(4, PlayerStatistics.CountMovedFrom(iron, 10, iron, 6));
        Assert.Equal(10, PlayerStatistics.CountMovedFrom(iron, 10, copper, 3));
        Assert.Equal(0, PlayerStatistics.CountMovedFrom(iron, 10, iron, 10));
        Assert.Equal(0, PlayerStatistics.CountMovedFrom(null, 0, iron, 5));
    }

    [Fact]
    public void Json_RoundTripKeepsEverything()
    {
        var stats = new PlayerStatistics();
        stats.StartSession(Now);
        stats.AddDistance(123.25, true);
        stats.RecordSpeed(33.5f);
        stats.RecordAsteroid(42);
        stats.RecordSector("1,2,3");
        stats.RecordDeath("Fell");
        stats.RecordItemUse("default:apple");
        stats.RecordEntityDamage(7.5);
        stats.AddPlayTime(125);
        stats.PrepareForSave(Now);

        var copy = JsonSerializer.Deserialize<PlayerStatistics>(JsonSerializer.Serialize(stats));
        copy.Normalize();

        Assert.Equal(123.25, copy.DistanceTraveled);
        Assert.Equal(123.25, copy.DistanceInShips);
        Assert.Equal(33.5f, copy.MaxSpeedReached);
        Assert.Equal(1, copy.AsteroidsDiscovered);
        Assert.Equal(1, copy.SectorsExplored);
        Assert.Equal(1, copy.DeathCauses["Fell"]);
        Assert.Equal(1, copy.ItemsUsed["default:apple"]);
        Assert.Equal(7.5, copy.EntityDamageDealt);
        Assert.Equal(125, copy.PlayTimeSeconds, 3);
        Assert.Equal(2, copy.TotalPlayTimeMinutes);
        Assert.Equal(1, copy.SessionsPlayed);
    }

    [Fact]
    public void Json_OldSaveStillLoads()
    {
        const string json = "{\"TotalPlayTimeMinutes\":12,\"SessionsPlayed\":40,\"DistanceTraveled\":350,\"EntityDamageDealt\":20,\"MaxSpeedReached\":0,\"AsteroidsDiscovered\":0,\"ItemsUsed\":null,\"DeathCauses\":null,\"BlocksPlaced\":9}";

        var stats = JsonSerializer.Deserialize<PlayerStatistics>(json);
        stats.StartSession(Now);

        Assert.Equal(350.0, stats.DistanceTraveled);
        Assert.Equal(20.0, stats.EntityDamageDealt);
        Assert.Equal(720.0, stats.PlayTimeSeconds);
        Assert.Equal(41, stats.SessionsPlayed);
        Assert.Equal(9, stats.BlocksPlaced);
        Assert.NotNull(stats.ItemsUsed);
        Assert.NotNull(stats.DeathCauses);
        Assert.Equal(0, stats.AsteroidsDiscovered);
    }

    [Fact]
    public void Normalize_FixesBrokenValues()
    {
        var stats = new PlayerStatistics
        {
            DistanceTraveled = double.NaN,
            DistanceInShips = -4,
            PlayTimeSeconds = double.PositiveInfinity,
            MaxSpeedReached = float.NaN,
            VisitedAsteroids = null,
            VisitedSectors = null
        };

        stats.Normalize();

        Assert.Equal(0, stats.DistanceTraveled);
        Assert.Equal(0, stats.DistanceInShips);
        Assert.Equal(0, stats.PlayTimeSeconds);
        Assert.Equal(0f, stats.MaxSpeedReached);
        Assert.Equal(0, stats.AsteroidsDiscovered);
        Assert.Equal(0, stats.SectorsExplored);
    }
}
