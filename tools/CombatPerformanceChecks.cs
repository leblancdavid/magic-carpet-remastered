using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Tools;

public partial class CombatPerformanceChecks : Node3D
{
    private int _assertions;
    private void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private async System.Threading.Tasks.Task CheckTileCollision(int radius)
    {
        var room = new Node3D();
        AddChild(room);
        var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius };
        room.AddChild(arena);
        var terrain = arena.GetChildren().OfType<PlanetTerrain>().Single();
        terrain.SetProcess(false);
        // Keep real physics retirement active while draining mesh builds manually.
        terrain.RebuildBudgetMilliseconds = 0;
        var observer = new Node3D();
        observer.AddToGroup("player");
        room.AddChild(observer);
        bool HasCollision(Vector3 up, bool accurate)
        {
            Vector3 point = arena.PlanetCenter + up * radius;
            float height = arena.HeightAt(point);
            using var ray = PhysicsRayQueryParameters3D.Create(arena.PlanetCenter + up * (radius + height + 40),
                arena.PlanetCenter + up * (radius + height - 40));
            using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            return hit.Count > 0 && hit["collider"].As<Node>().IsInGroup("deformable_terrain")
                && (!accurate || Mathf.Abs(((Vector3)hit["position"]).DistanceTo(arena.PlanetCenter) - radius - height) < 0.35f);
        }
        void Drain()
        {
            int steps = 0, largest = 0;
            while (terrain.PendingPatchCount > 0 && steps++ < 20000)
            {
                terrain._Process(0);
                largest = Math.Max(largest, terrain.FrameLargestTileResolution);
            }
            Check(terrain.PendingPatchCount == 0 && largest <= PlanetTerrain.MaxTileResolution,
                $"Radius {radius}: fine/coarse transitions drain with bounded atomic tile resolution");
        }
        foreach (Vector3 up in new[] { Vector3.Up, new Vector3(1, 1, 0).Normalized(), Vector3.Down })
        {
            observer.GlobalPosition = arena.OnSurface(arena.PlanetCenter + up * radius, 6);
            terrain.RefreshDetail();
            arena.FlattenTerrain(observer.GlobalPosition, 45, 7);
            terrain._Process(0);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(HasCollision(up, false), $"Radius {radius}: old collider covers queued fine replacement");
            Drain();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(terrain.RetiringCollisionCount == 0, "Old LOD colliders retire after replacement enters physics");
            foreach (MeshInstance3D instance in terrain.GetChildren().OfType<MeshInstance3D>().Where(m => m.Visible && m.Name != "Ocean"))
            {
                using var mesh = (ArrayMesh)instance.Mesh;
                if (mesh.GetSurfaceCount() != 1) throw new InvalidOperationException("Ground and visual skirts must use one render surface.");
                using var arrays = mesh.SurfaceGetArrays(0);
                var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                int resolution = (int)Math.Round(Math.Sqrt(4 + indices.Length / 6.0) - 2);
                int groundCount = (resolution + 1) * (resolution + 1);
                if (vertices.Length != groundCount + resolution * 24 || normals.Length != vertices.Length)
                    throw new InvalidOperationException("Combined surface lost ground or skirt vertices.");
                for (int vertex = groundCount; vertex < vertices.Length; vertex += 3)
                {
                    Vector3 expected = (vertices[vertex + 2] - vertices[vertex]).Cross(vertices[vertex + 1] - vertices[vertex]).Normalized();
                    if (normals[vertex].Dot(expected) < 0.999f || normals[vertex + 1].Dot(expected) < 0.999f || normals[vertex + 2].Dot(expected) < 0.999f)
                        throw new InvalidOperationException("Combined skirt normals lost clockwise flat shading.");
                }
            }
            Check(true, $"Radius {radius}: all visible ground/skirt surfaces preserve vertex counts and clockwise flat normals");
            Vector3 tangent = arena.Tangent(Vector3.Right, observer.GlobalPosition).Normalized();
            foreach (float offset in new[] { -0.5f, 0f, 0.5f })
                Check(HasCollision((up + tangent * offset / radius).Normalized(), true),
                    $"Radius {radius}: real fine collision is continuous across tile/face seams");
            float before = arena.HeightAt(observer.GlobalPosition);
            int shapesBefore = terrain.GetChildren().OfType<StaticBody3D>().Sum(body => body.GetChildren().OfType<CollisionShape3D>().Count());
            arena.ApplyCrater(observer.GlobalPosition, 5, 2);
            Check(Mathf.IsEqualApprox(arena.HeightAt(observer.GlobalPosition), before - 2),
                "Tile scheduling retains immediate crater heights");
            // Repeated pending changes must commit the latest chronological state.
            arena.RaiseTerrain(observer.GlobalPosition, 5, 1);
            Drain();
            Check(HasCollision(up, false), "Live edit replacement retains collision before the next physics publication");
            Check(terrain.RetiringCollisionCount > 0, "Edited live shapes enter delayed retirement");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(HasCollision(up, true), "Coalesced crater/raise updates reach actual tile collision");
            Check(terrain.RetiringCollisionCount == 0 &&
                terrain.GetChildren().OfType<StaticBody3D>().Sum(body => body.GetChildren().OfType<CollisionShape3D>().Count()) == shapesBefore,
                "Edited old shapes retire without accumulating collision children");
        }
        // Return from the underside after unloading the edited home tiles.
        observer.GlobalPosition = arena.OnSurface(arena.PlanetCenter + Vector3.Up * radius, 6);
        terrain.RefreshDetail();
        Drain();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(HasCollision(Vector3.Up, true), "Fine/coarse/fine cycling retains edits and collision");
        observer.RemoveFromGroup("player");
        room.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    public override async void _Ready()
    {
        try
        {
            var pool = EquipmentImpactPool.Prepare(this);
            var orange = new Color(1, 0.4f, 0.1f);
            var cyan = new Color(0.1f, 0.8f, 1);
            var firstBurst = pool.Emit(Vector3.Zero, orange, orange, 0.28f, 0.4f, 2.2f);
            var simultaneous = pool.Emit(Vector3.One, cyan, cyan, 0.28f, 0.4f, 2.2f);
            firstBurst._Process(0.14);
            var otherMaterial = (StandardMaterial3D)simultaneous.GetChild<MeshInstance3D>(0).MaterialOverride;
            Check(otherMaterial.AlbedoColor == cyan, "Simultaneous pooled effects retain independent colors and fade state");
            firstBurst._Process(1);
            Check(!firstBurst.Visible && !firstBurst.IsProcessing() && !firstBurst.IsQueuedForDeletion(), "Finished pooled effect sleeps instead of being freed");
            var reused = pool.Emit(new Vector3(3, 4, 5), cyan, cyan, 0.45f, 0.8f, 3.5f);
            Check(reused == firstBurst && reused.Visible && reused.Scale == Vector3.One * 0.8f
                && reused.GlobalPosition == new Vector3(3, 4, 5), "Reuse resets pose, scale and visibility");
            reused._Process(0.1);
            Check(reused.Visible && !reused.IsQueuedForDeletion(), "Reuse resets age and duration");
            reused._Process(1); simultaneous._Process(1);
            var orbs = new List<AntimatterPickup>();
            for (int i = 0; i < 512; i++)
            {
                var orb = new AntimatterPickup { Position = new Vector3((i % 32) * 10 - 160, 2, (i / 32) * 10 - 80), AntimatterAmount = i % 20 + 1 };
                AddChild(orb);
                orb.SetPhysicsProcess(false);
                orb.Freeze = true;
                orbs.Add(orb);
            }
            var candidates = new List<AntimatterPickup>();
            foreach (var center in new[] { Vector3.Zero, new Vector3(-160, 2, -80), new Vector3(150, 2, 70), new Vector3(-8, 2, -8) })
            {
                AntimatterNeighborhood.Query(GetTree(), center, 6, candidates);
                foreach (var orb in orbs)
                    if (orb.GlobalPosition.DistanceTo(center) <= 6 + orb.Radius)
                        Check(candidates.Contains(orb), "Broad phase must contain every exact sphere hit, including negative cell boundaries");
            }
            Check(candidates.Count < orbs.Count / 4, "Local query rejects most distant antimatter spheres");
            Check(orbs[0].GetChild<MeshInstance3D>(1).Mesh == orbs[511].GetChild<MeshInstance3D>(1).Mesh,
                "Antimatter globes share one immutable visual mesh");
            var large = new AntimatterPickup { Position = new Vector3(70, 2, 0), AntimatterAmount = 100000 };
            AddChild(large); large.Freeze = true; large.SetPhysicsProcess(false);
            AntimatterNeighborhood.Query(GetTree(), Vector3.Zero, 6, candidates);
            Check(candidates.Contains(large), "Huge neighbor radius is included in the broad phase");
            large.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            AntimatterNeighborhood.Query(GetTree(), Vector3.Zero, 6, candidates);
            Check(candidates.TrueForAll(GodotObject.IsInstanceValid), "Freed wrappers are removed before queries return candidates");

            // CPU broad-phase microbenchmark only; no frame-rate or GPU claim.
            var timer = Stopwatch.StartNew();
            long indexedBytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) AntimatterNeighborhood.Query(GetTree(), Vector3.Zero, 6, candidates);
            indexedBytes = GC.GetAllocatedBytesForCurrentThread() - indexedBytes;
            double indexedMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            long scanBytes = GC.GetAllocatedBytesForCurrentThread();
            int hits = 0;
            for (int i = 0; i < 1000; i++)
                foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
                    if (node is AntimatterPickup orb && orb.GlobalPosition.DistanceTo(Vector3.Zero) <= 6 + orb.Radius) hits++;
            scanBytes = GC.GetAllocatedBytesForCurrentThread() - scanBytes;
            GD.Print($"Antimatter lookup CPU sample (512 globes / 1000 queries): indexed {indexedMs:0.00} ms, {indexedBytes} managed bytes; full group scan {timer.Elapsed.TotalMilliseconds:0.00} ms, {scanBytes} managed bytes; hits {hits}.");
            foreach (var orb in orbs) orb.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = 500 };
            AddChild(arena);
            var terrain = arena.GetChildren().OfType<PlanetTerrain>().Single();
            Vector3 point = arena.PlanetCenter + Vector3.Up * 500;
            float before = arena.HeightAt(point);
            timer.Restart();
            arena.ApplyCrater(point, 4.5f, 2);
            GD.Print($"Crater registration CPU sample: {timer.Elapsed.TotalMilliseconds:0.00} ms; pending patches {terrain.PendingPatchCount}.");
            Check(Mathf.IsEqualApprox(arena.HeightAt(point), before - 2), "Crater height becomes authoritative immediately");
            int pending = terrain.PendingPatchCount;
            Check(pending > 1, "Seam crater queues multiple affected patches");
            terrain.RebuildBudgetMilliseconds = 0;
            terrain._Process(0);
            Check(terrain.PendingPatchCount == pending - 1, "Exhausted frame budget processes one patch rather than draining the queue");
            Check(terrain.FrameRebuiltPatchCount == 1 && terrain.FrameRebuildMilliseconds >= 0,
                "Per-frame profiling reports the actual bounded terrain batch");
            Check(Mathf.IsEqualApprox(arena.HeightAt(point), before - 2), "Queued collision does not delay live deformation height");
            while (terrain.PendingPatchCount > 0) terrain._Process(0);
            Check(terrain.PendingPatchCount == 0, "Deferred patch queue eventually drains");
            terrain._Process(0);
            Check(terrain.FrameRebuiltPatchCount == 0 && terrain.FrameRebuildMilliseconds == 0,
                "Idle profiling clears stale rebuild cost and patch counts");
            arena.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            foreach (int radius in PlanetSizes.Radii) await CheckTileCollision(radius);
            GD.Print($"Combat performance checks passed: {_assertions} assertions.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
