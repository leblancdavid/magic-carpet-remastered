using System.Diagnostics;
using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Only instantiated by the profiler; normal gameplay pays no timing overhead.
public partial class ProfiledManaPickup : ManaPickup
{
    public static double LogicMilliseconds { get; private set; }
    public static double IntegrationMilliseconds { get; private set; }
    public static int LogicCalls { get; private set; }
    public static int IntegrationCalls { get; private set; }

    public static void ResetMeasurements()
    {
        LogicMilliseconds = IntegrationMilliseconds = 0;
        LogicCalls = IntegrationCalls = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        long start = Stopwatch.GetTimestamp();
        base._PhysicsProcess(delta);
        LogicMilliseconds += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        LogicCalls++;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        long start = Stopwatch.GetTimestamp();
        base._IntegrateForces(state);
        IntegrationMilliseconds += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        IntegrationCalls++;
    }
}
