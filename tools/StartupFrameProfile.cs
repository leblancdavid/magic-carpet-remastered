using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Includes the first frame, ordinary active gameplay, and a same-seed new match.
public partial class StartupFrameProfile : Node
{
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private async Task Run()
    {
        try
        {
            PlanetSizes.SelectedIndex = OS.GetCmdlineUserArgs().Contains("--xl") ? 3 : 1;
            bool quick = OS.GetCmdlineUserArgs().Contains("--quick");
            for (int run = 0; run < (quick ? 1 : 2); run++)
            {
                WorldSeeds.UseSeed(7319);
                var timer = Stopwatch.StartNew();
                var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<Game>();
                AddChild(game);
                GD.Print($"Startup run {run + 1}: creation {timer.Elapsed.TotalMilliseconds:0.00} ms");
                var terrain = game.GetNode<HeightmapArena>("HeightmapArena").GetChildren().OfType<PlanetTerrain>().Single();
                var samples = new List<double>();
                var session = Stopwatch.StartNew(); timer.Restart();
                int window = 0, rebuilt = 0, peakPending = terrain.PendingPatchCount;
                long allocated = GC.GetTotalAllocatedBytes();
                double rebuildMax = 0, physicsMax = 0, processMax = 0;
                while (session.Elapsed.TotalSeconds < (quick ? 15 : 60))
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    samples.Add(timer.Elapsed.TotalMilliseconds); timer.Restart();
                    rebuilt += terrain.FrameRebuiltPatchCount;
                    rebuildMax = Math.Max(rebuildMax, terrain.FrameRebuildMilliseconds);
                    physicsMax = Math.Max(physicsMax, Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000);
                    processMax = Math.Max(processMax, Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000);
                    peakPending = Math.Max(peakPending, terrain.PendingPatchCount);
                    if (session.Elapsed.TotalSeconds < (window + 1) * 15) continue;
                    samples.Sort(); var counts = terrain.GetDetailCounts();
                    GD.Print($"Startup run {run + 1} window {window * 15}-{(window + 1) * 15}s: "
                        + $"{samples.Count} samples, p95/max {samples[(int)(samples.Count * 0.95)]:0.00}/{samples[^1]:0.00} ms; "
                        + $"rebuilt {rebuilt}, pending peak/current {peakPending}/{terrain.PendingPatchCount}, "
                        + $"fine/tiles/meshes {counts.FineParents}/{counts.BuiltTiles}/{counts.PublishedMeshes}; "
                        + $"rebuild/physics/process max {rebuildMax:0.0}/{physicsMax:0.0}/{processMax:0.0} ms; "
                        + $"alloc {(GC.GetTotalAllocatedBytes() - allocated) / 1048576.0:0.0} MiB");
                    samples.Clear(); rebuilt = 0; peakPending = terrain.PendingPatchCount;
                    allocated = GC.GetTotalAllocatedBytes(); window++;
                    rebuildMax = physicsMax = processMax = 0;
                }
                game.QueueFree();
                for (int i = 0; i < 12; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            }
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
