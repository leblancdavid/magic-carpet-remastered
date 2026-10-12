using System;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class CompactTerrainChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message)
    { _checks++; if (!value) throw new Exception(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }

    private async Task Run()
    {
        try
        {
            foreach (int radius in PlanetSizes.Radii)
            {
                var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius }; AddChild(arena);
                int ocean = 0, steep = 0, lowlands = 0, land = 0, comfortable = 0;
                float lowest = float.PositiveInfinity, highest = float.NegativeInfinity;
                Vector3 peak = Vector3.Up;
                const int samples = 4096;
                for (int i = 0; i < samples; i++)
                {
                    float y = 1 - 2 * (i + 0.5f) / samples, a = i * 2.39996323f, r = Mathf.Sqrt(1 - y * y);
                    Vector3 up = new(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
                    Vector3 point = arena.PlanetCenter + up * radius;
                    float height = arena.HeightAt(point);
                    Check(float.IsFinite(height), "Terrain samples are finite on every hemisphere");
                    lowest = Mathf.Min(lowest, height);
                    if (height > highest) { highest = height; peak = up; }
                    if (height < arena.WaterHeight) ocean++;
                    if (height > arena.WaterHeight && height < 7) lowlands++;
                    Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
                    float dx = (arena.SurfaceHeight(point + frame.X) - arena.SurfaceHeight(point - frame.X)) / 2;
                    float dz = (arena.SurfaceHeight(point + frame.Z) - arena.SurfaceHeight(point - frame.Z)) / 2;
                    if (height > arena.WaterHeight)
                    {
                        land++;
                        float slope = new Vector2(dx, dz).Length();
                        if (slope <= Mathf.Tan(Mathf.DegToRad(20))) comfortable++;
                        if (slope > Mathf.Tan(Mathf.DegToRad(40))) steep++;
                    }
                }
                GD.Print($"Terrain {radius}: {lowest:0.0}..{highest:0.0} m, ocean {100f * ocean / samples:0.0}%, comfortable land {100f * comfortable / land:0.0}%, impassable land {100f * steep / land:0.0}%");
                Check(ocean > samples * 0.15f && ocean < samples * 0.75f && lowest < arena.WaterHeight - 3,
                    "World retains substantial deep ocean and land rather than becoming all hills");
                Check(highest > Mathf.Max(12, radius * 0.06f) && lowlands > samples * 0.15f && steep > 0,
                    "Each size retains low combat terrain and occasional route-blocking mountain slopes");
                Check(comfortable >= land * 0.85f && steep <= land * 0.05f,
                    "At least 85% of raw land is under 20 degrees and at most 5% exceeds the 40-degree hover limit");
                foreach (Vector3 seam in new[] { new Vector3(1, 1, 0), new Vector3(0, -1, 1), new Vector3(-1, 0, -1) })
                {
                    Vector3 up = seam.Normalized();
                    float a = arena.HeightAt(arena.PlanetCenter + (up + Vector3.Right * 0.000001f).Normalized() * radius);
                    float b = arena.HeightAt(arena.PlanetCenter + (up - Vector3.Right * 0.000001f).Normalized() * radius);
                    Check(Mathf.Abs(a - b) < 0.01f, "Layered terrain remains continuous at cube-face seams");
                }
                var layout = PlanetLayout.Create(arena);
                arena.PreparePlanetLayout(layout);
                CheckPreparedSlopes(arena);
                foreach (var site in layout.EconomicSites)
                {
                    float floor = arena.HeightAt(arena.PlanetCenter + site.Up * radius);
                    if (floor <= arena.WaterHeight)
                        Check(floor < arena.WaterHeight - 0.4f, "Floating station pad stays below ocean mesh without coplanar terrain");
                }
                foreach (Vector3 up in layout.Starts)
                {
                    Vector3 center = PlanetLayout.Position(arena, up);
                    float height = arena.HeightAt(center);
                    Check(height > arena.WaterHeight + 3, "Both starting pads are dry with useful ocean clearance");
                    Basis frame = PlanetGeometry.Frame(up, Vector3.Forward);
                    foreach (Vector3 offset in new[] { frame.X * 12, -frame.X * 12, frame.Z * 12, -frame.Z * 12 })
                        Check(Mathf.Abs(arena.HeightAt(center + offset) - height) < 0.15f,
                            "Base, ship and collector footprint is genuinely level rather than pole-smoothed");
                    using var ray = PhysicsRayQueryParameters3D.Create(arena.OnSurface(center, 5), arena.OnSurface(center, -2));
                    await Frames(4);
                    using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
                    Check(hit.Count > 0 && Mathf.Abs(arena.Clearance(hit["position"].AsVector3())) < 0.4f,
                        "Prepared pad render/physics surface agrees after collider publication");
                }
                Vector3 mountain = arena.OnSurface(arena.PlanetCenter + peak * radius);
                float before = arena.HeightAt(mountain);
                arena.ApplyCrater(mountain, 6, 2);
                Check(Mathf.Abs(arena.HeightAt(mountain) - before + 2) < 0.01f,
                    "Mountain terrain retains exact deformation semantics after landing pads");
                if (DisplayServer.GetName() != "headless" && radius <= 200)
                {
                    var camera = new Camera3D { Current = true, Far = 3000 };
                    AddChild(camera);
                    camera.GlobalPosition = arena.PlanetCenter + peak * (radius * 2.5f);
                    camera.LookAt(arena.PlanetCenter, Mathf.Abs(peak.Dot(Vector3.Up)) < 0.9f ? Vector3.Up : Vector3.Right);
                    var light = new DirectionalLight3D { RotationDegrees = new Vector3(-45, 35, 0), LightEnergy = 1.5f }; AddChild(light);
                    await Frames(4); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var image = GetViewport().GetTexture().GetImage();
                    Check(image.SavePng($"C:/Users/lblan/AppData/Local/Temp/opencode/CompactTerrain{radius}.png") == Error.Ok,
                        "Rendered whole-planet terrain capture saves");
                    camera.QueueFree(); light.QueueFree();
                }
                arena.QueueFree(); await Frames(4);
            }
            GD.Print($"Compact terrain checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private void CheckPreparedSlopes(HeightmapArena arena)
    {
        int land = 0, comfortable = 0, blocked = 0;
        float maximumHeight = float.NegativeInfinity;
        for (int i = 0; i < 4096; i++)
        {
            float y = 1 - 2 * (i + 0.5f) / 4096, a = i * 2.39996323f, r = Mathf.Sqrt(1 - y * y);
            Vector3 up = new(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
            Vector3 point = arena.PlanetCenter + up * arena.PlanetRadius;
            float height = arena.HeightAt(point);
            maximumHeight = Mathf.Max(maximumHeight, height);
            if (height <= arena.WaterHeight) continue;
            land++;
            Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
            float dx = (arena.SurfaceHeight(point + frame.X) - arena.SurfaceHeight(point - frame.X)) / 2;
            float dz = (arena.SurfaceHeight(point + frame.Z) - arena.SurfaceHeight(point - frame.Z)) / 2;
            float slope = new Vector2(dx, dz).Length();
            if (slope <= Mathf.Tan(Mathf.DegToRad(20))) comfortable++;
            if (slope > Mathf.Tan(Mathf.DegToRad(40))) blocked++;
        }
        GD.Print($"Prepared {arena.PlanetRadius}: comfortable land {100f * comfortable / land:0.0}%, impassable land {100f * blocked / land:0.0}%, remaining peak {maximumHeight:0.0} m");
        Check(comfortable >= land * 0.85f && blocked <= land * 0.05f,
            "Destination pads preserve the comfortable-land and impassable-land budgets");
        Check(maximumHeight > 10 && blocked > 0, "Prepared world retains occasional substantial mountains rather than becoming entirely flat");
    }
}
