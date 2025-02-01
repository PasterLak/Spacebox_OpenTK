namespace Spacebox.Engine.Utils;

using System;

public class FastRandom
{
    private static long _globalSeed = DateTime.UtcNow.Ticks;
    private ulong _state;

    public FastRandom()
    {
        long seed = Interlocked.Increment(ref _globalSeed);
        _state = (ulong)seed;

        if (_state == 0)
            _state = 88172645463325252UL;
    }
    public FastRandom(ulong seed) // > 0
    {
        if (seed == 0)
            seed = 88172645463325252UL; 
        
        _state = seed;
    }
    public ulong NextULong()
    {
        ulong x = _state;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        _state = x;
        return x;
    }

    public uint NextUInt()
    {
        return (uint)(NextULong() >> 32);
    }

    public int NextInt()
    {
        return (int)(NextULong() >> 32);
    }

    /// <summary>
    /// Random float between 0-1
    /// </summary>
    public float NextFloat()
    {
        return (NextULong() >> 40) * (1.0f / (1UL << 24));
    }

    /// <summary>
    // /// Random double between 0-1
    // /// </summary>
    public double NextDouble()
    {
        // Используем верхние 53 бита для создания double
        return (NextULong() >> 11) * (1.0 / (1UL << 53));
    }
    
    public int Next(int min, int max)
    {
        if (min >= max)
        {
            var m = min;
            min = max;
            max = m;
        }

        long range = (long)max - min;
        return (int)(NextULong() % (ulong)range) + min;
    }

    /// <summary>
    // /// Random int between 0-max
    // /// </summary>
    public int Next(int max)
    {
        if (max <= 0)
            throw new ArgumentException("Max should be greater than zero.");

        return (int)(NextULong() % (ulong)max);
    }

    public bool NextBool()
    {
        return (NextULong() & 1) == 1;
    }
}
