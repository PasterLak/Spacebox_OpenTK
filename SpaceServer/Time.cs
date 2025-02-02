using System;

namespace SpaceServer;
public static class Time
{

    private static int _lastTick;
    public static int StartTime;
    public static float Delta { get; private set; }

     static Time()
    {
     
        _lastTick = Environment.TickCount;

        Delta = 0;
    }

    public static void Update()
    {

        int currentTick = Environment.TickCount;

        int deltaMilliseconds = currentTick - _lastTick;

        Delta = deltaMilliseconds / 1000.0f;

        _lastTick = currentTick;

       
    }
}
