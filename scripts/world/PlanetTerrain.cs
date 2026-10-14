using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Cube-sphere patches: shared direction-based height evaluation keeps edits
// consistent across face boundaries. Fine collision/detail follows world actors.
public partial class PlanetTerrain : Node3D
{
    public HeightmapArena Arena { get; set; } = null!;
    private const int PatchesPerFace = 8;
    public const int MaxTileResolution = 25;
    private sealed record Edit(Vector3 Up, float Radius, float Amount, bool Flatten);
    private sealed class Patch
    {
        public int Face, Resolution, DesiredResolution, FineResolution;
        public float X, Y, Span = 1;
        public Patch? Parent;
        public readonly List<Patch> Tiles = new();
        public bool TilesPublished;
        public Vector3 Up;
        public float Bound;
        public MeshInstance3D Mesh = null!;
        public CollisionShape3D Collision = null!;
        public Vector3[] Vertices = System.Array.Empty<Vector3>();
        public Vector3[] Normals = System.Array.Empty<Vector3>();
        public Color[] Colors = System.Array.Empty<Color>();
        public readonly List<Edit> PendingEdits = new();
    }
    private readonly List<Patch> _patches = new();
    // Mesh/shape setters copy into native packed arrays synchronously. These
    // build-only buffers can be reused without changing published resources.
    private readonly Dictionary<int, (int[] Indices, Vector3[] Faces)> _buildBuffers = new();
    private const float EditCellSize = 32;
    private readonly Dictionary<Vector3I, List<Edit>> _editCells = new();
    private readonly HashSet<Patch> _dirty = new();
    private readonly Dictionary<CollisionShape3D, (int Ticks, bool FreeBody, bool FreeShape)> _retiringCollisions = new();
    private readonly List<CollisionShape3D> _retirementBuffer = new();
    private readonly FastNoiseLite _noise = new() { Seed = 27031, Frequency = 0.004f, FractalOctaves = 3 };
    private readonly FastNoiseLite _continents = new() { Seed = 27031, Frequency = 0.004f, FractalOctaves = 2 };
    private sealed record MountainRegion(Vector3 Up, Basis Frame);
    private readonly List<MountainRegion> _mountainRegions = new();
    private sealed record LandingPad(Vector3 Up, float Inner, float Outer, float Height);
    private readonly List<LandingPad> _landingPads = new();
    private sealed record OpeningApproach(Vector3 From, Vector3 To, Vector3 Tangent, Vector3 Normal, float Angle);
    private readonly List<OpeningApproach> _openingApproaches = new();
    private float _mountainFoldAmplitude;
    private readonly StandardMaterial3D _material = new() { AlbedoColor = new Color(0.65f, 0.65f, 0.65f), VertexColorUseAsAlbedo = true, Roughness = 1 };
    private float _refreshTimer;
    public int PendingPatchCount => _dirty.Count;
    public int RetiringCollisionCount => _retiringCollisions.Count;
    public double RebuildBudgetMilliseconds { get; set; } = 2;
    public double FrameRebuildMilliseconds { get; private set; }
    public int FrameRebuiltPatchCount { get; private set; }
    public int FrameLargestTileResolution { get; private set; }
    public PlanetLayout? GeneratedLayout { get; private set; }
    public string GenerationError { get; private set; } = string.Empty;
    public int GenerationAttempts { get; private set; }

    public override void _ExitTree()
    {
        _noise.Dispose();
        _continents.Dispose();
        _material.Dispose();
    }

    // Read only by diagnostic tools, outside the terrain rebuild hot path.
    public (int FineParents, int BuiltTiles, int PublishedMeshes) GetDetailCounts()
    {
        int parents = 0, tiles = 0, meshes = 0;
        foreach (Patch patch in _patches)
        {
            if (patch.Tiles.Count > 0) parents++;
            if (patch.Mesh.Visible) meshes++;
            foreach (Patch tile in patch.Tiles)
                if (tile.Resolution > 0)
                {
                    tiles++;
                    if (tile.Mesh.Visible) meshes++;
                }
        }
        return (parents, tiles, meshes);
    }

    public override void _Ready()
    {
        if (Arena.GenerateSeededLayout)
        {
            if (!float.IsFinite(Arena.MinimumStartAngle) || Arena.MinimumStartAngle <= 0 || Arena.MinimumStartAngle > 180)
            { GenerationError = "Faction separation must be greater than zero and at most 180 degrees."; return; }
            for (int attempt = 0; attempt < 16; attempt++)
            {
                GenerationAttempts = attempt + 1;
                _landingPads.Clear(); _openingApproaches.Clear(); _mountainFoldAmplitude = 0;
                Arena.TerrainSeed = WorldSeeds.Derive(Arena.WorldSeed, 1 + attempt * 8);
                ConfigureNoise(); PrepareMountainRegions();
                var layout = PlanetLayout.Create(Arena, WorldSeeds.Derive(Arena.WorldSeed, 2 + attempt * 8), Arena.MinimumStartAngle);
                PrepareLayout(layout);
                if (!PlanetGenerationValidation.Accept(Arena, layout, out string reason))
                { GenerationError = reason; continue; }
                GeneratedLayout = layout; GenerationError = string.Empty; break;
            }
            if (GeneratedLayout == null) return;
        }
        else { ConfigureNoise(); PrepareMountainRegions(); }
        for (int face = 0; face < 6; face++)
        for (int y = 0; y < PatchesPerFace; y++)
        for (int x = 0; x < PatchesPerFace; x++)
        {
            Patch patch = new() { Face = face, X = x, Y = y, Up = Direction(face, x + 0.5f, y + 0.5f) };
            SetBound(patch);
            _patches.Add(patch);
            BuildPatch(patch, 4);
        }
        float sea = Arena.PlanetRadius + Arena.WaterHeight;
        AddChild(new MeshInstance3D
        {
            Name = "Ocean",
            Position = Arena.PlanetCenter,
            Mesh = new SphereMesh { Radius = sea, Height = sea * 2, RadialSegments = 512, Rings = 256 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.15f, 0.3f), Roughness = 0.35f }
        });
        RefreshDetail();
        // Loading establishes actor collision before gameplay starts. Runtime
        // detail changes and craters use the bounded queue instead.
        while (_dirty.Count > 0) RebuildDirty();
    }

    private void ConfigureNoise()
    {
        // Preserve the historical fixed-seed diagnostics while real matches use
        // a world-derived seed and an independent fine-detail stream.
        _continents.Seed = Arena.TerrainSeed;
        _noise.Seed = Arena.TerrainSeed == 27031 ? 27031 : WorldSeeds.Derive(Arena.TerrainSeed, 1);
    }

    public float HeightAt(Vector3 up)
    {
        Vector3 point = up * Arena.PlanetRadius;
        float height = ProfileHeight(up);
        foreach (LandingPad pad in _landingPads)
        {
            float distance = up.DistanceTo(pad.Up) * Arena.PlanetRadius;
            if (distance < pad.Outer)
            {
                // Blend land approaches toward the water surface, not the deep
                // seabed hidden beneath it, which otherwise makes a steep rim.
                float background = Mathf.Max(height, Arena.WaterHeight - 0.5f);
                height = Mathf.Lerp(pad.Height, background, Ease(pad.Inner, pad.Outer, distance));
            }
        }
        height += MountainFolds(up, height);
        if (!_editCells.TryGetValue(EditCell(point), out var edits)) return height;
        foreach (Edit edit in edits)
        {
            float chord = up.DistanceTo(edit.Up) * Arena.PlanetRadius;
            if (chord >= edit.Radius) continue;
            float t = chord / edit.Radius;
            float weight = 1 - t * t * (3 - 2 * t);
            height = edit.Flatten ? Mathf.Lerp(height, edit.Amount, weight)
                : Mathf.Max(Arena.WaterHeight - 2, height + edit.Amount * weight);
        }
        return height;
    }

    private static float Ease(float low, float high, float value)
    {
        float t = Mathf.Clamp((value - low) / (high - low), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private float GentleHeight(Vector3 up)
    {
        float continent = _continents.GetNoise3Dv(up * 320);
        float height = Arena.WaterHeight + 0.4f + continent * Mathf.Min(Arena.PlanetRadius * 0.05f, 12);
        // Low-amplitude secondary shapes provide islands and soft hills, rather
        // than noisy cliff edges throughout otherwise ordinary land.
        height += _noise.GetNoise3Dv(up * 520 + Vector3.One * 137) * 0.8f;
        height += _noise.GetNoise3Dv(up * Arena.PlanetRadius * 2) * 0.35f;
        return height < Arena.WaterHeight ? Arena.WaterHeight + (height - Arena.WaterHeight) * 3 : height;
    }

    private float ProfileHeight(Vector3 up)
    {
        float height = GentleHeight(up);
        foreach (MountainRegion region in _mountainRegions) height += MountainHeight(up, region);
        return height;
    }

    private void PrepareMountainRegions()
    {
        _mountainRegions.Clear();
        // Three separated land-biased regions; compact worlds need more shoulder
        // area because their protected destination ramps cover a larger fraction.
        for (int region = 0; region < 3; region++)
        {
            Vector3 best = Vector3.Up;
            float bestHeight = float.NegativeInfinity;
            for (int i = 0; i < 96; i++)
            {
                float y = 1 - 2 * (i + 0.5f) / 96, angle = i * 2.39996323f, r = Mathf.Sqrt(1 - y * y);
                Vector3 up = new(r * Mathf.Cos(angle), y, r * Mathf.Sin(angle));
                if (_mountainRegions.Exists(existing => up.Dot(existing.Up) > 0.5f)) continue;
                float height = GentleHeight(up);
                if (height <= bestHeight) continue;
                bestHeight = height; best = up;
            }
            _mountainRegions.Add(new MountainRegion(best,
                PlanetGeometry.Frame(best, Mathf.Abs(best.Z) < 0.9f ? Vector3.Forward : Vector3.Right)));
        }
    }

    private float MountainHeight(Vector3 up, MountainRegion region)
    {
        float compact = 1 - Mathf.Clamp((Arena.PlanetRadius - 100) / 100, 0, 1);
        float reach = Arena.PlanetRadius * (0.38f + 0.06f * compact);
        float distance = up.DistanceTo(region.Up) * Arena.PlanetRadius;
        if (distance >= reach) return 0;
        float foothills = 1 - Ease(0, reach, distance);
        float across = Mathf.Abs(up.Dot(region.Frame.X)) * Arena.PlanetRadius;
        float along = up.Dot(region.Frame.Z) * Arena.PlanetRadius;
        float ridge = 1 - Ease(0, Mathf.Clamp(Arena.PlanetRadius * 0.055f, 6, 18), across);
        float pass = Ease(0, 5, Mathf.Abs(along - reach * 0.2f));
        // Concentrate relief into a broader steep shoulder within each region,
        // keeping its outskirts gentle and its summit height near the old budget.
        float shoulder = 1 - Ease(Arena.PlanetRadius * (0.25f + 0.06f * compact),
            Arena.PlanetRadius * (0.33f + 0.06f * compact), distance);
        return Mathf.Clamp(Arena.PlanetRadius * 0.1f, 10, 40)
            * (0.85f * shoulder + 0.1f * foothills + 0.05f * shoulder * ridge * pass);
    }

    private float MountainFolds(Vector3 up, float height)
    {
        if (_mountainFoldAmplitude <= 0 || _landingPads.Count == 0 || height <= Arena.WaterHeight + 1) return 0;
        float folds = 0;
        foreach (MountainRegion region in _mountainRegions)
        {
            float distance = up.DistanceTo(region.Up) * Arena.PlanetRadius;
            float envelope = 1 - Ease(Arena.PlanetRadius * 0.15f, Arena.PlanetRadius * 0.34f, distance);
            if (envelope <= 0) continue;
            float across = up.Dot(region.Frame.X) * Arena.PlanetRadius;
            float along = up.Dot(region.Frame.Z) * Arena.PlanetRadius;
            // Curving, uneven ridges rather than a regular grid of bumps.
            folds += envelope * (0.75f * Mathf.Sin(across / 1.5f + 1.2f * Mathf.Sin(along / 9))
                + 0.25f * Mathf.Sin((along + across * 0.4f) / 2.3f));
        }
        if (folds == 0) return 0;
        float protection = 1;
        foreach (LandingPad pad in _landingPads)
            protection *= Ease(pad.Inner + 3, pad.Inner + 15, up.DistanceTo(pad.Up) * Arena.PlanetRadius);
        foreach (OpeningApproach approach in _openingApproaches)
        {
            float along = Mathf.Atan2(up.Dot(approach.Tangent), up.Dot(approach.From));
            float distance = along >= 0 && along <= approach.Angle ? Mathf.Abs(up.Dot(approach.Normal)) * Arena.PlanetRadius
                : Mathf.Min(up.DistanceTo(approach.From), up.DistanceTo(approach.To)) * Arena.PlanetRadius;
            protection *= Ease(6, 14, distance);
        }
        return folds * _mountainFoldAmplitude * protection * Ease(Arena.WaterHeight + 1, Arena.WaterHeight + 3, height);
    }

    private void TuneMountainFolds()
    {
        // Startup only. Different layout pads protect different portions of the
        // mountains, so tune the remaining folds to ~4% of actual exposed land.
        float low = 0, high = 3, best = 0, bestError = float.PositiveInfinity;
        _mountainFoldAmplitude = 0;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            int land = 0, steep = 0;
            for (int i = 0; i < 2048; i++)
            {
                float y = 1 - 2 * (i + 0.5f) / 2048, a = i * 2.39996323f, r = Mathf.Sqrt(1 - y * y);
                Vector3 up = new(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
                Vector3 point = Arena.PlanetCenter + up * Arena.PlanetRadius;
                if (Arena.HeightAt(point) <= Arena.WaterHeight) continue;
                land++;
                Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
                float x = (Arena.SurfaceHeight(point + frame.X) - Arena.SurfaceHeight(point - frame.X)) / 2;
                float z = (Arena.SurfaceHeight(point + frame.Z) - Arena.SurfaceHeight(point - frame.Z)) / 2;
                if (new Vector2(x, z).Length() > Mathf.Tan(Mathf.DegToRad(40))) steep++;
            }
            float coverage = (float)steep / Mathf.Max(1, land), error = Mathf.Abs(coverage - 0.04f);
            if (error < bestError) { bestError = error; best = _mountainFoldAmplitude; }
            if (coverage >= 0.037f && coverage <= 0.043f) return;
            if (coverage < 0.04f) low = _mountainFoldAmplitude; else high = _mountainFoldAmplitude;
            _mountainFoldAmplitude = (low + high) / 2;
        }
        _mountainFoldAmplitude = best;
    }

    public void PrepareLayout(PlanetLayout layout)
    {
        _landingPads.Clear();
        _mountainFoldAmplitude = 0;
        _openingApproaches.Clear();
        for (int side = 0; side < 2; side++)
        {
            void Approach(Vector3 to)
            {
                Vector3 from = layout.Starts[side];
                Vector3 tangent = (to - from * from.Dot(to)).Normalized();
                _openingApproaches.Add(new OpeningApproach(from, to, tangent, from.Cross(tangent).Normalized(),
                    Mathf.Acos(Mathf.Clamp(from.Dot(to), -1, 1))));
            }
            Approach(layout.AntimatterFields[side]);
            for (int role = 0; role < 4; role++) Approach(layout.EconomicSites[side * 4 + role].Up);
        }
        void Pad(Vector3 up, float inner, float outer, bool dry)
        {
            float natural = ProfileHeight(up);
            // Floating destinations flatten the seabed below the water mesh;
            // a terrain pad exactly at sea level produces coplanar z-fighting.
            float height = dry ? Mathf.Clamp(natural, Arena.WaterHeight + 3.5f, Arena.WaterHeight + 5)
                : natural > Arena.WaterHeight ? Mathf.Min(natural, Arena.WaterHeight + 5) : Arena.WaterHeight - 0.5f;
            // Lower summit destinations into the surrounding accessible layer,
            // and give larger height differences proportionately longer ramps.
            float difference = Mathf.Abs(Mathf.Max(natural, Arena.WaterHeight - 0.5f) - height);
            Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
            for (int i = 0; i < 8; i++)
            {
                Vector3 tangent = frame.X.Rotated(up, Mathf.Tau * i / 8);
                Vector3 edge = PlanetGeometry.TravelRotation(up, tangent, Arena.PlanetRadius, outer) * up;
                float surrounding = ProfileHeight(edge);
                surrounding = Mathf.Max(surrounding, Arena.WaterHeight - 0.5f);
                difference = Mathf.Max(difference, Mathf.Abs(surrounding - height));
            }
            outer = Mathf.Max(outer, inner + difference * 4);
            _landingPads.Add(new LandingPad(up, inner, outer, height));
        }
        foreach (var site in layout.EconomicSites) Pad(site.Up, 8, 24, false);
        foreach (Vector3 up in layout.AntimatterFields) Pad(up, 14, 32, false);
        foreach (var site in layout.Landmarks) Pad(site.Up, 6, 24, false);
        // Bases take final priority: overlapping approaches must not tilt the
        // ship/collector footprint inside an otherwise flat starting pad.
        foreach (Vector3 up in layout.Starts) Pad(up, 20, 45, true);
        TuneMountainFolds();
        // Startup only: invalidate both visible fine surfaces and dormant coarse
        // caches so later LOD changes cannot resurrect the pre-pad landscape.
        foreach (Patch patch in _patches)
        {
            patch.Vertices = System.Array.Empty<Vector3>();
            if (patch.Tiles.Count == 0) _dirty.Add(patch);
            foreach (Patch tile in patch.Tiles) { tile.Vertices = System.Array.Empty<Vector3>(); _dirty.Add(tile); }
        }
        while (_dirty.Count > 0) RebuildDirty();
    }

    public override void _PhysicsProcess(double delta)
    {
        // A shape enabled during rendering may not enter the physics broad phase
        // until the next physics step. Keep the old LOD collider through that step.
        _retirementBuffer.Clear();
        _retirementBuffer.AddRange(_retiringCollisions.Keys);
        foreach (CollisionShape3D collision in _retirementBuffer)
        {
            if (!IsInstanceValid(collision))
            {
                _retiringCollisions.Remove(collision);
                continue;
            }
            var retirement = _retiringCollisions[collision];
            if (retirement.Ticks > 1)
                _retiringCollisions[collision] = (retirement.Ticks - 1, retirement.FreeBody, retirement.FreeShape);
            else
            {
                collision.Disabled = true;
                if (retirement.FreeBody) collision.GetParent().QueueFree();
                else if (retirement.FreeShape) collision.QueueFree();
                _retiringCollisions.Remove(collision);
            }
        }
        _refreshTimer -= (float)delta;
        if (_refreshTimer <= 0)
        {
            _refreshTimer = 0.5f;
            RefreshDetail();
        }
    }

    public override void _Process(double delta)
    {
        FrameRebuildMilliseconds = 0;
        FrameRebuiltPatchCount = 0;
        FrameLargestTileResolution = 0;
        if (_dirty.Count > 0) RebuildDirty();
        if (FrameRebuiltPatchCount > 0) FrameRebuildMilliseconds = Arena.LastRebuildMilliseconds;
    }

    public void RefreshDetail()
    {
        List<(Vector3 Up, float Range, bool Ship)> observers = new();
        foreach (string group in new[] { "player", "rival_ship", "loose_antimatter", "ecosystem_enemy", "simple_monster", "drone", "guardian-drone" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D actor && !actor.IsQueuedForDeletion() && node is not AntimatterPickup { IsAvailable: false })
                    observers.Add((Arena.UpAt(actor.GlobalPosition),
                        group is "player" or "rival_ship" ? 125 : 12, group is "player" or "rival_ship"));
        foreach (Patch patch in _patches)
        {
            // Seeded games don't spend startup work refining an arbitrary north pole.
            // Game warms the actual actor layout after both factions are created.
            bool nearby = observers.Count == 0 && !Arena.GenerateSeededLayout
                && patch.Up.DistanceTo(Vector3.Up) * Arena.PlanetRadius < 180 + patch.Bound;
            bool actorSupport = false;
            foreach (var observer in observers)
                if (patch.Up.DistanceTo(observer.Up) * Arena.PlanetRadius <
                    (observer.Ship && patch.FineResolution < Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2
                        ? 100 : observer.Range) + patch.Bound)
                {
                    actorSupport = true;
                    if (observer.Ship) { nearby = true; break; }
                }
            // Fixed world-space resolution (about 2 units at face center).
            // Distant actors need local support, not a 125m high-detail visual bubble.
            int resolution = nearby ? Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2
                : actorSupport ? Mathf.Clamp(Mathf.CeilToInt(Arena.PlanetRadius / 128) * 2, 4, 8) : 4;
            patch.DesiredResolution = resolution;
            if (resolution > 8)
            {
                _dirty.Remove(patch);
                if (patch.Tiles.Count == 0) PrepareTiles(patch, resolution);
            }
            else if (patch.Tiles.Count > 0 || patch.Resolution != resolution) _dirty.Add(patch);
        }
    }

    public void CompleteInitialDetail()
    {
        RefreshDetail();
        while (_dirty.Count > 0) RebuildDirty();
    }

    public int Deform(Vector3 up, float radius, float amount, bool flatten)
    {
        if (radius <= 0) return 0;
        var edit = new Edit(up, radius, amount, flatten);
        Vector3 center = up * Arena.PlanetRadius;
        Vector3I min = EditCell(center - Vector3.One * radius);
        Vector3I max = EditCell(center + Vector3.One * radius);
        for (int z = min.Z; z <= max.Z; z++)
        for (int y = min.Y; y <= max.Y; y++)
        for (int x = min.X; x <= max.X; x++)
        {
            Vector3I cell = new(x, y, z);
            if (!_editCells.TryGetValue(cell, out var edits)) _editCells[cell] = edits = new List<Edit>();
            edits.Add(edit); // Preserve chronological flatten/additive semantics.
        }
        int editedVertices = 0;
        foreach (Patch patch in _patches)
        {
            if (up.DistanceTo(patch.Up) * Arena.PlanetRadius > radius + 2 + patch.Bound) continue;
            // An active fine patch is rebuilt tile-by-tile. Pending initial tiles
            // evaluate the latest authoritative edit history when they are built.
            if (patch.Tiles.Count > 0 && patch.DesiredResolution > 4)
            {
                // Invalidate the dormant coarse cache rather than retaining an
                // ever-growing pending list while the fine surface stays active.
                patch.Vertices = System.Array.Empty<Vector3>();
                patch.PendingEdits.Clear();
                foreach (Patch tile in patch.Tiles)
                    if (up.DistanceTo(tile.Up) * Arena.PlanetRadius <= radius + 2 + tile.Bound)
                    {
                        _dirty.Add(tile);
                        tile.PendingEdits.Add(edit);
                    }
            }
            else
            {
                _dirty.Add(patch);
                patch.PendingEdits.Add(edit);
            }
            // Diagnostic estimate only: height changes are exact and immediate.
            // Avoid resampling entire fine patches merely to count edited vertices.
            float spacing = Arena.PlanetRadius / (4f * Mathf.Max(4, patch.DesiredResolution));
            editedVertices += Mathf.Max(1, Mathf.CeilToInt(Mathf.Pi * radius * radius / (spacing * spacing)));
        }
        return editedVertices;
    }

    private void RebuildDirty()
    {
        ulong start = Time.GetTicksUsec();
        int rebuilt = 0;
        while (_dirty.Count > 0)
        {
            Patch? selected = null;
            // Fine actor-supporting collision takes precedence over distant LOD.
            foreach (Patch candidate in _dirty)
                if (selected == null || candidate.DesiredResolution > selected.DesiredResolution) selected = candidate;
            _dirty.Remove(selected!);
            BuildPatch(selected!, Mathf.Max(4, selected!.DesiredResolution));
            FrameLargestTileResolution = Mathf.Max(FrameLargestTileResolution, selected!.Resolution);
            if (selected.Parent is Patch parent)
            {
                if (!parent.TilesPublished && parent.Tiles.TrueForAll(t => t.Resolution > 0 && !_dirty.Contains(t)))
                {
                    // Publish the complete replacement before removing its old
                    // collider. No holes while initial fine tiles are queued.
                    foreach (Patch tile in parent.Tiles)
                    {
                        tile.Mesh.Visible = true;
                        EnableCollision(tile.Collision);
                    }
                    parent.Mesh.Visible = false;
                    _retiringCollisions[parent.Collision] = (2, false, false);
                    parent.TilesPublished = true;
                }
            }
            else if (selected.Tiles.Count > 0)
            {
                // A coarse replacement is ready. Retire the fine geometry only
                // now, not when the LOD request first enters the queue.
                selected.Mesh.Visible = true;
                EnableCollision(selected.Collision);
                foreach (Patch tile in selected.Tiles)
                {
                    _dirty.Remove(tile);
                    if (tile.Resolution == 0) continue;
                    tile.Mesh.Visible = false;
                    _retiringCollisions[tile.Collision] = (2, true, false);
                    tile.Mesh.QueueFree();
                }
                selected.Tiles.Clear();
                selected.TilesPublished = false;
                selected.FineResolution = 0;
            }
            rebuilt++;
            if ((Time.GetTicksUsec() - start) / 1000.0 >= RebuildBudgetMilliseconds) break;
        }
        FrameRebuiltPatchCount += rebuilt;
        Arena.ReportPlanetRebuild(rebuilt, (Time.GetTicksUsec() - start) / 1000.0);
    }

    private static Vector3I EditCell(Vector3 point) => new(Mathf.FloorToInt(point.X / EditCellSize),
        Mathf.FloorToInt(point.Y / EditCellSize), Mathf.FloorToInt(point.Z / EditCellSize));

    private void EnableCollision(CollisionShape3D collision)
    {
        _retiringCollisions.Remove(collision);
        collision.Disabled = false;
    }

    private void SetBound(Patch patch)
    {
        patch.Bound = 0;
        foreach (Vector2 corner in new[] { Vector2.Zero, Vector2.Right, Vector2.Down, Vector2.One })
            patch.Bound = Mathf.Max(patch.Bound, Arena.PlanetRadius * patch.Up.DistanceTo(
                Direction(patch.Face, patch.X + corner.X * patch.Span, patch.Y + corner.Y * patch.Span)));
        patch.Bound *= 1.1f;
    }

    private void PrepareTiles(Patch parent, int resolution)
    {
        // Pick a divisor, preserving every vertex of the original fine grid.
        int divisions = Mathf.Max(2, Mathf.CeilToInt((float)resolution / MaxTileResolution));
        while (resolution % divisions != 0) divisions++;
        parent.FineResolution = resolution;
        float span = 1f / divisions;
        for (int y = 0; y < divisions; y++)
        for (int x = 0; x < divisions; x++)
        {
            Patch tile = new() { Parent = parent, Face = parent.Face,
                X = parent.X + x * span, Y = parent.Y + y * span, Span = span,
                DesiredResolution = resolution / divisions };
            tile.Up = Direction(tile.Face, tile.X + span * 0.5f, tile.Y + span * 0.5f);
            SetBound(tile);
            parent.Tiles.Add(tile);
            _dirty.Add(tile);
        }
    }

    private void BuildPatch(Patch patch, int resolution)
    {
        bool replaceLiveCollision = patch.Resolution > 0 && !patch.Collision.Disabled;
        if (patch.Resolution == 0)
        {
            patch.Mesh = new MeshInstance3D { Visible = patch.Parent == null };
            AddChild(patch.Mesh);
            var body = new StaticBody3D();
            body.AddToGroup("deformable_terrain");
            patch.Collision = new CollisionShape3D { Disabled = patch.Parent != null };
            body.AddChild(patch.Collision);
            AddChild(body);
        }
        int side = resolution + 1;
        bool fullRebuild = patch.Resolution != resolution || patch.Vertices.Length == 0;
        patch.Resolution = resolution;
        // Ground vertices stay first; the tail contains the same flat-shaded
        // skirt triangles, sharing one material/surface and cached edit buffers.
        Vector3[] vertices = fullRebuild ? new Vector3[side * side + resolution * 24] : patch.Vertices;
        Vector3[] normals = fullRebuild ? new Vector3[vertices.Length] : patch.Normals;
        Color[] colors = fullRebuild ? new Color[vertices.Length] : patch.Colors;
        if (!_buildBuffers.TryGetValue(resolution, out var buffers))
            _buildBuffers[resolution] = buffers = (new int[resolution * resolution * 6 + resolution * 24],
                new Vector3[resolution * resolution * 6]);
        int[] indices = buffers.Indices;
        for (int y = 0; y <= resolution; y++)
        for (int x = 0; x <= resolution; x++)
        {
            int index = y * side + x;
            Vector3 up = Direction(patch.Face, patch.X + (float)x / resolution * patch.Span, patch.Y + (float)y / resolution * patch.Span);
            if (!fullRebuild)
            {
                bool affected = false;
                foreach (Edit edit in patch.PendingEdits)
                {
                    float reach = (edit.Radius + 2) / Arena.PlanetRadius;
                    if (up.DistanceSquaredTo(edit.Up) <= reach * reach) { affected = true; break; }
                }
                if (!affected) continue;
            }
            float height = HeightAt(up);
            vertices[index] = Arena.PlanetCenter + up * (Arena.PlanetRadius + height);
            Vector3 tangent = up.Cross(Mathf.Abs(up.Y) < 0.9f ? Vector3.Up : Vector3.Right).Normalized();
            Vector3 bitangent = up.Cross(tangent);
            float dx = HeightAt((up + tangent / Arena.PlanetRadius).Normalized()) - HeightAt((up - tangent / Arena.PlanetRadius).Normalized());
            float dy = HeightAt((up + bitangent / Arena.PlanetRadius).Normalized()) - HeightAt((up - bitangent / Arena.PlanetRadius).Normalized());
            normals[index] = (up - tangent * dx * 0.5f - bitangent * dy * 0.5f).Normalized();
            colors[index] = height < 0.5f ? new Color(0.62f, 0.52f, 0.28f)
                : height < 6 ? new Color(0.22f, 0.42f, 0.16f)
                : height < 10 ? new Color(0.32f, 0.48f, 0.22f) : new Color(0.44f, 0.42f, 0.35f);
        }
        patch.Vertices = vertices;
        patch.Normals = normals;
        patch.Colors = colors;
        patch.PendingEdits.Clear();
        int cursor = 0;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            int a = y * side + x, b = a + 1, c = a + side, d = c + 1;
            WriteTriangle(a, b, c);
            WriteTriangle(b, d, c);
        }
        void WriteTriangle(int a, int b, int c)
        {
            if ((vertices[b] - vertices[a]).Cross(vertices[c] - vertices[a]).Dot(vertices[a] - Arena.PlanetCenter) > 0) (b, c) = (c, b);
            indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = c;
        }
        int skirtVertex = side * side;
        // Visual skirts close the geometric gap between different patch LODs.
        // Nearby colliders share the same fine sampling, including cube-face edges.
        for (int i = 0; i < resolution; i++)
        {
            Edge(i, i + 1); Edge(resolution * side + i + 1, resolution * side + i);
            Edge((i + 1) * side, i * side); Edge(i * side + resolution, (i + 1) * side + resolution);
        }
        void Edge(int a, int b)
        {
            Vector3 bottomA = vertices[a] - (vertices[a] - Arena.PlanetCenter).Normalized() * 8;
            Vector3 bottomB = vertices[b] - (vertices[b] - Arena.PlanetCenter).Normalized() * 8;
            SkirtTriangle(vertices[a], vertices[b], bottomA, colors[a], colors[b], colors[a]);
            SkirtTriangle(vertices[b], bottomB, bottomA, colors[b], colors[b], colors[a]);
        }
        void SkirtTriangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            // Godot uses clockwise front faces. Keep independent triangle
            // vertices so skirt normals remain flat, as SurfaceTool produced.
            Vector3 normal = (c - a).Cross(b - a).Normalized();
            vertices[skirtVertex] = a; colors[skirtVertex] = ca; normals[skirtVertex] = normal;
            indices[cursor++] = skirtVertex++;
            vertices[skirtVertex] = b; colors[skirtVertex] = cb; normals[skirtVertex] = normal;
            indices[cursor++] = skirtVertex++;
            vertices[skirtVertex] = c; colors[skirtVertex] = cc; normals[skirtVertex] = normal;
            indices[cursor++] = skirtVertex++;
        }
        using Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        // Native nodes retain their own references after assignment.
        using ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        patch.Mesh.Mesh = mesh;
        patch.Mesh.MaterialOverride = _material;
        Vector3[] faces = buffers.Faces;
        for (int i = 0; i < faces.Length; i++) faces[i] = vertices[indices[i]];
        using var shape = new ConcavePolygonShape3D { Data = faces };
        if (replaceLiveCollision)
        {
            // Editing an existing tile has the same broad-phase publication
            // delay as a LOD swap. Preserve old support through a physics step.
            CollisionShape3D previous = patch.Collision;
            patch.Collision = new CollisionShape3D { Shape = shape };
            previous.GetParent().AddChild(patch.Collision);
            _retiringCollisions[previous] = (2, false, true);
        }
        else patch.Collision.Shape = shape;
    }

    private static Vector3 Direction(int face, float x, float y)
    {
        float u = x * 2 / PatchesPerFace - 1, v = y * 2 / PatchesPerFace - 1;
        return (face switch
        {
            0 => new Vector3(u, 1, v), 1 => new Vector3(u, -1, -v),
            2 => new Vector3(1, v, u), 3 => new Vector3(-1, v, -u),
            4 => new Vector3(u, v, -1), _ => new Vector3(-u, v, 1)
        }).Normalized();
    }
}
