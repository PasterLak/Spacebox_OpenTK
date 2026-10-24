namespace Spacebox.Network.Ships;

public static class ShipStateRelevance
{
    public const float NearDistance = 250f;
    public const float MidDistance = 1000f;
    public const int NearInterval = 1;
    public const int MidInterval = 4;
    public const int FarInterval = 20;

    public static int IntervalFor(float distanceSquared)
    {
        if (distanceSquared < NearDistance * NearDistance) return NearInterval;
        if (distanceSquared < MidDistance * MidDistance) return MidInterval;
        return FarInterval;
    }

    public static bool IsDue(long tick, long entityId, int interval)
    {
        if (interval <= 1) return true;
        long phase = (tick + (entityId & 0x7FFFFFFF)) % interval;
        return phase == 0;
    }
}
