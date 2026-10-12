using System;

namespace MagicCarpetRemastered.Scripts.World;

// Session selection survives scene reloads. Each subsystem owns its own stream.
public static class WorldSeeds
{
    private static int _current;
    private static bool _selected;
    public static int Current => _selected ? _current : NewWorld();
    public static void UseSeed(int seed) { _current = seed; _selected = true; }
    public static int NewWorld()
    {
        int next;
        do next = (int)Random.Shared.NextInt64(int.MinValue, (long)int.MaxValue + 1);
        while (_selected && next == _current);
        UseSeed(next); return next;
    }

    public static int Derive(int seed, int stream)
    {
        unchecked
        {
            uint value = (uint)seed ^ ((uint)stream * 0x9E3779B9u);
            value ^= value >> 16; value *= 0x85EBCA6Bu;
            value ^= value >> 13; value *= 0xC2B2AE35u;
            return (int)(value ^ (value >> 16));
        }
    }
}
