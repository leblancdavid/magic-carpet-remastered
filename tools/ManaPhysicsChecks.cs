using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Real physics frames: catches contact/solver behavior that manual tick checks cannot.
public partial class ManaPhysicsChecks : Node3D
{
    private int _checks;

    public override void _Ready()
    {
        Callable.From(() => { _ = RunChecks(); }).CallDeferred();
    }

    private async Task RunChecks()
    {
        try
        {
            await CheckGroundAndSlope();
            await ClearActors();
            await CheckObstacle();
            await ClearActors();
            await CheckMerging();
            await ClearActors();
            await CheckMergeGrowth();
            await ClearActors();
            await CheckCarryAndBalloonOnlyCollection();
            await ClearActors();
            await CheckLiveTerrain();
            await ClearActors();
            CheckSupportCache();
            await ClearActors();
            await CheckCastleSpills();
            GD.Print($"Mana physics checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private T Add<T>(T actor, Vector3 position) where T : Node
    {
        if (actor is Node3D spatial) spatial.Position = position;
        AddChild(actor);
        return actor;
    }

    private StaticBody3D AddFloor(float angle = 0)
    {
        var floor = Add(new StaticBody3D(), new Vector3(0, -0.5f, 0));
        floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) } });
        floor.Rotation = new Vector3(0, 0, angle);
        return floor;
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task ClearActors()
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        await Frames(2);
    }

    private async Task CheckGroundAndSlope()
    {
        AddFloor();
        var orb = Add(new ManaPickup(), new Vector3(0, 4, 0));
        await Frames(180);
        Check(Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.06f, "Loose sphere rests touching ground without bobbing");
        float y = orb.GlobalPosition.Y;
        await Frames(30);
        Check(Mathf.Abs(orb.GlobalPosition.Y - y) < 0.02f, "Resting sphere does not hover or oscillate");
        float radius = orb.Radius;
        orb.ManaAmount = 80;
        Check(orb.Radius > radius * 3.8f, "Eight times mana produces a dramatically larger globe");
        Check(Mathf.IsEqualApprox(radius, (0.35f / 3) * Mathf.Pow(10, 0.65f)), "Ten-mana globe is exactly one-third its previous radius");
        var fragment = new ManaPickup { ManaAmount = 2 };
        Check(fragment.Radius > 0.18f && fragment.Radius < 0.19f, "Two-mana fragments use the reduced overall scale");
        Check(radius > fragment.Radius * 2.8f, "Ten-mana globes visibly dwarf two-mana fragments");
        fragment.ManaAmount = 1;
        Check(fragment.Radius < 0.4f, "Single-mana fragments remain visibly smaller than two-mana fragments");
        fragment.Free();
        var collision = orb.GetChild<CollisionShape3D>(0);
        Check(Mathf.IsEqualApprox(((SphereShape3D)collision.Shape).Radius, orb.Radius), "Physical shape scales with visible sphere");
        orb.TakeMana(70);
        Check(Mathf.IsEqualApprox(orb.Radius, radius), "Partial transfer shrinks sphere and collider");

        await ClearActors();
        AddFloor(0.25f);
        orb = Add(new ManaPickup(), new Vector3(0, 1, 0));
        await Frames(150);
        Check(orb.GlobalPosition.X < -1.0f, "Gravity rolls mana downhill");
        Check(orb.AngularVelocity.Length() > 0.1f, "Slope movement produces physical sphere rotation");
    }

    private void CheckSupportCache()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 24, CellSize = 2,
            HeightScale = 0, WaterHeight = -20 }, Vector3.Zero);
        arena.FlattenTerrain(Vector3.Zero, 100, 0);
        var orb = Add(new ManaPickup { Freeze = true }, Vector3.Zero);
        orb.SetPhysicsProcess(false);
        var grounded = typeof(ManaPickup).GetMethod("IsGrounded", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool Supported() => (bool)grounded.Invoke(orb, null)!;
        orb.GlobalPosition = new Vector3(0, orb.Radius, 0);
        Check(Supported() && Supported(), "Repeated same-tick support checks agree");
        orb.GlobalPosition += Vector3.Up * 2;
        Check(!Supported(), "Same-tick movement invalidates support cache");
        orb.GlobalPosition = new Vector3(0, orb.Radius, 0);
        Check(Supported(), "Same-tick return to surface recomputes support");
        orb.LinearVelocity = Vector3.Up * 3;
        Check(!Supported(), "Same-tick upward velocity invalidates support cache");
        orb.LinearVelocity = Vector3.Zero;
        Check(Supported(), "Restored velocity permits supported contact");
        ulong revision = arena.SurfaceRevision;
        arena.LowerTerrain(Vector3.Zero, 5, 3);
        Check(arena.SurfaceRevision > revision && !Supported(), "Immediate terrain edit invalidates cached support before collision rebuild");
        arena.RaiseTerrain(Vector3.Zero, 5, 3);
        Check(Supported(), "Same-tick restored terrain recomputes support");
        orb.GlobalPosition = new Vector3(0, orb.Radius + 0.3f, 0);
        Check(Supported(), "Ground support accepts its configured contact skin");
        orb.ManaAmount = 1;
        Check(!Supported(), "Same-tick shrinking radius invalidates support cache");
        arena.WaterHeight = 2;
        Check(Supported(), "Same-tick water-height change invalidates support cache");
        orb.BeginCarry(this, Vector3.Zero);
        Check(!Supported(), "Carried mana never reuses a supported result");
    }

    private async Task CheckObstacle()
    {
        AddFloor();
        var castle = Add(new CastleKeep { InitialStoredMana = 0 }, new Vector3(0, 2.5f, 0));
        var orb = Add(new ManaPickup { Ownership = ManaOwnership.Enemy }, new Vector3(-8, 0.5f, 0));
        orb.LinearVelocity = new Vector3(8, 0, 0);
        await Frames(90);
        Check(orb.GlobalPosition.X < -4.0f, "Mana cannot pass through castle collider");
        Check(castle.StoredMana == 0 && orb.ManaAmount == 10, "Physical castle collision does not deposit mana");

        var wall = Add(new StaticBody3D(), new Vector3(10, 2, 0));
        wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1, 4, 12) } });
        orb = Add(new ManaPickup(), new Vector3(6, 0.5f, 0));
        orb.LinearVelocity = new Vector3(8, 0, 0);
        await Frames(60);
        Check(orb.GlobalPosition.X < 9.5f - orb.Radius + 0.05f, "Mana collides with ordinary world objects");
    }

    private async Task CheckCastleSpills()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 24, CellSize = 2, HeightScale = 0, WaterHeight = -20 }, Vector3.Zero);
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var castle = Add(new CastleKeep { IsPlayerCastle = false, InitialStoredMana = 37, MaxHealth = 100 }, new Vector3(0, 2.5f, 0));
        var tracker = Add(new WorldManaTracker(), Vector3.Zero);
        await Frames(2);
        castle.ApplyDamage(100);
        await Frames(120);
        tracker.Refresh();
        Check(tracker.TotalMana == 37 && tracker.NeutralLooseMana == 37, "Destroyed castle spills conserve mana through real solver frames");
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup orb || !orb.IsAvailable) continue;
            Check(Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.06f, "Castle spill settles on heightmap terrain");
            Vector3 offset = orb.GlobalPosition - castle.GlobalPosition;
            float distanceToFootprint = new Vector2(Mathf.Max(0, Mathf.Abs(offset.X) - 4),
                Mathf.Max(0, Mathf.Abs(offset.Z) - 4)).Length();
            Check(distanceToFootprint >= orb.Radius - 0.06f,
                "Castle spill remains outside the actual box collider after settling and attraction");
        }
    }

    private async Task CheckMerging()
    {
        AddFloor();
        var first = Add(new ManaPickup { ManaAmount = 10 }, new Vector3(-2, 0.5f, 0));
        var second = Add(new ManaPickup { ManaAmount = 30 }, new Vector3(2, 0.7f, 0));
        first.LinearVelocity = new Vector3(4, 0, 0);
        second.LinearVelocity = new Vector3(-4, 0, 0);
        var tracker = Add(new WorldManaTracker(), Vector3.Zero);
        await Frames(30);
        Check(AvailableCount() == 2, "Moving collision does not immediately merge mana");
        await Frames(360);
        tracker.Refresh();
        Check(AvailableCount() == 1 && first.ManaAmount == 40, "Same-color settled contact merges into one sphere");
        Check(tracker.TotalMana == 40 && tracker.NeutralLooseMana == 40, "Merge conserves amount and ownership exactly");
        Check(first.Radius > 1.2f && first.Mass == 4, "Merged sphere grows in size and mass");
        var third = Add(new ManaPickup { ManaAmount = 7 }, first.GlobalPosition + Vector3.Right * (first.Radius + 0.1f));
        await Frames(360);
        tracker.Refresh();
        Check(first.ManaAmount == 47 && AvailableCount() == 1 && tracker.TotalMana == 47, "Chained contacts merge once without duplicate mana");

        await ClearActors();
        AddFloor();
        first = Add(new ManaPickup(), new Vector3(-0.4f, 0.5f, 0));
        second = Add(new ManaPickup { Ownership = ManaOwnership.Enemy }, new Vector3(0.4f, 0.5f, 0));
        await Frames(30);
        Check(AvailableCount() == 2 && first.ManaAmount == 10 && second.ManaAmount == 10, "Different colors collide without merging");
        // Reposition into touching contact, then change color: no new entry required.
        second.GlobalPosition = first.GlobalPosition + Vector3.Right * (first.Radius + second.Radius - 0.02f);
        second.ReassignOwnership(ManaOwnership.Neutral);
        await Frames(60);
        Check(AvailableCount() == 2, "Ownership change still requires sustained settled contact");
        await Frames(360);
        Check(AvailableCount() == 1 && first.ManaAmount == 20, "Ownership change allows matching contact to merge");

        foreach (ManaOwnership owner in new[] { ManaOwnership.Player, ManaOwnership.Enemy })
        {
            await ClearActors();
            AddFloor();
            first = Add(new ManaPickup { Ownership = owner, ManaAmount = 11 }, new Vector3(-0.4f, 0.5f, 0));
            second = Add(new ManaPickup { Ownership = owner, ManaAmount = 13 }, new Vector3(0.4f, 0.5f, 0));
            await Frames(420);
            Check(AvailableCount() == 1 && first.ManaAmount == 24 && first.Ownership == owner, $"{owner} spheres merge and retain their color");
        }

        await ClearActors();
        AddFloor();
        var sizeProbe = new ManaPickup();
        float smallRadius = sizeProbe.Radius;
        sizeProbe.Free();
        first = Add(new ManaPickup(), new Vector3(-smallRadius - 2, smallRadius, 0));
        second = Add(new ManaPickup(), new Vector3(smallRadius + 2, smallRadius, 0));
        float initialDistance = first.GlobalPosition.DistanceTo(second.GlobalPosition);
        await Frames(60);
        Check(AvailableCount() == 2 && first.GlobalPosition.DistanceTo(second.GlobalPosition) < initialDistance - 0.5f,
            "Magnetism visibly closes a four-unit gap between grounded same-color spheres");
        await Frames(600);
        Check(AvailableCount() == 1 && first.ManaAmount == 20,
            $"Separated spheres magnetically reach contact and merge without manual repositioning (first {first.GlobalPosition}, speed {first.LinearVelocity}; second {(IsInstanceValid(second) ? second.GlobalPosition : Vector3.Zero)}, amount {first.ManaAmount})");
        second = Add(new ManaPickup(), first.GlobalPosition + Vector3.Right * 10);
        // Break contact and move beyond magnetic range; earlier contact must not count.
        first.SpillAt(new Vector3(-10, first.Radius, 0));
        second.SpillAt(new Vector3(10, second.Radius, 0));
        await Frames(180);
        Check(AvailableCount() == 2, "Separated spheres reset contact progress and stay outside magnetic range");
        second.SpillAt(first.GlobalPosition + Vector3.Right * (first.Radius + second.Radius - 0.01f));
        await Frames(60);
        Check(AvailableCount() == 2, "Recontact starts a fresh full merge delay");
        await Frames(180);
        Check(AvailableCount() == 1 && first.ManaAmount == 30, "Settled recontact eventually merges with exact mana");

        await ClearActors();
        AddFloor();
        first = Add(new ManaPickup(), new Vector3(-smallRadius, smallRadius, 0));
        second = Add(new ManaPickup(), new Vector3(smallRadius, smallRadius, 0));
        for (int i = 0; i < 240; i++)
        {
            if (first.IsAvailable) first.LinearVelocity = Vector3.Forward * 2;
            if (IsInstanceValid(second) && second.IsAvailable) second.LinearVelocity = Vector3.Forward * 2;
            await Frames(1);
        }
        Check(AvailableCount() == 1 && first.ManaAmount == 20,
            "Touching grounded spheres moving together merge despite shared rolling speed");

        await ClearActors();
        AddFloor();
        first = Add(new ManaPickup(), new Vector3(-smallRadius - 2, smallRadius, 0));
        second = Add(new ManaPickup { Ownership = ManaOwnership.Enemy }, new Vector3(smallRadius + 2, smallRadius, 0));
        await Frames(180);
        Check(Mathf.Abs(first.GlobalPosition.X + smallRadius + 2) < 0.05f
            && Mathf.Abs(second.GlobalPosition.X - smallRadius - 2) < 0.05f,
            "Different ownership spheres do not magnetically approach");

        await ClearActors();
        AddFloor();
        var wall = Add(new StaticBody3D(), new Vector3(0, 3, 0));
        wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.5f, 6, 20) } });
        first = Add(new ManaPickup(), new Vector3(-smallRadius - 1, smallRadius, 0));
        second = Add(new ManaPickup(), new Vector3(smallRadius + 1, smallRadius, 0));
        await Frames(480);
        Check(AvailableCount() == 2 && first.GlobalPosition.X <= -smallRadius - 0.2f
            && second.GlobalPosition.X >= smallRadius + 0.2f,
            "Magnetic attraction cannot pull mana through a wall or merge across it");

        await ClearActors();
        AddFloor();
        first = Add(new ManaPickup { GravityScale = 0 }, new Vector3(-smallRadius - 1, 10, 0));
        second = Add(new ManaPickup { GravityScale = 0 }, new Vector3(smallRadius + 1, 10, 0));
        await Frames(180);
        Check(AvailableCount() == 2 && Mathf.Abs(first.GlobalPosition.X + smallRadius + 1) < 0.05f
            && Mathf.Abs(second.GlobalPosition.X - smallRadius - 1) < 0.05f,
            "Airborne mana does not magnetically converge before reaching ground support");
    }

    private async Task CheckCarryAndBalloonOnlyCollection()
    {
        AddFloor();
        var hauler = Add(new Node3D(), new Vector3(0, 5, 0));
        var cargo = Add(new ManaPickup(), Vector3.Zero);
        cargo.BeginCarry(hauler, Vector3.Down);
        var loose = Add(new ManaPickup(), new Vector3(0, 4, 0));
        await Frames(30);
        Check(cargo.IsCarried && cargo.ManaAmount == 10 && cargo.CollisionLayer == 0 && cargo.Freeze, "Cargo disables physics and cannot merge with loose mana");
        Check(cargo.GlobalPosition.IsEqualApprox(hauler.GlobalPosition + Vector3.Down), "Frozen cargo follows hauler");
        cargo.SpillAt(new Vector3(5, 4, 0));
        await Frames(120);
        Check(!cargo.IsCarried && !cargo.Freeze && cargo.CollisionLayer == 1 && Mathf.Abs(cargo.GlobalPosition.Y - cargo.Radius) < 0.06f, "Released cargo resumes gravity and collisions");

        await ClearActors();
        AddFloor();
        var player = Add(new CarpetFlightController(), new Vector3(0, 4, 0));
        player.SetPhysicsProcess(false);
        player.SetProcess(false);
        var orb = Add(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 19 }, new Vector3(6, 4, 0));
        var tracker = Add(new WorldManaTracker(), Vector3.Zero);
        int claimed = player.ClaimedMana;
        int mana = player.Mana;
        await Frames(90);
        tracker.Refresh();
        Check(orb.IsAvailable && Mathf.Abs(orb.GlobalPosition.X - 6) < 0.05f && Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.06f, "Possessed mana falls to ground instead of homing to nearby player");

        player.GlobalPosition = new Vector3(0, 1, 0);
        orb.GlobalPosition = player.GlobalPosition + new Vector3(0.5f, 0, 0);
        await Frames(60);
        tracker.Refresh();
        Check(orb.IsAvailable && orb.ManaAmount == 19 && player.ClaimedMana == claimed && player.Mana == mana, "Actual player contact does not collect or refill mana");
        Check(tracker.TotalMana == 19 && tracker.PlayerControlledMana == 19, "Player contact leaves possessed mana in world accounting");

        var castle = Add(new CastleKeep { InitialStoredMana = 0 }, new Vector3(-12, 2.5f, 0));
        var balloon = Add(new ManaBalloon { CarryCapacity = 1 }, orb.GlobalPosition + Vector3.Up * 4.5f);
        await Frames(240);
        tracker.Refresh();
        Check(castle.StoredMana == 19 && balloon.CarriedCount == 0 && player.ClaimedMana == claimed + 19, "Balloon alone collects possessed mana and delivers castle power");
        Check(tracker.TotalMana == 19 && tracker.PlayerControlledMana == 19, "Balloon-only delivery conserves world mana");
    }

    private async Task CheckLiveTerrain()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 24, CellSize = 2, HeightScale = 0, WaterHeight = -20 }, Vector3.Zero);
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var orb = Add(new ManaPickup { ManaAmount = 80 }, new Vector3(0, 12, 0));
        await Frames(60);
        Check(Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.08f, "Seeded mana starts at amount-scaled ground clearance");
        arena.RaiseTerrain(Vector3.Zero, 8, 3);
        await Frames(60);
        Check(orb.GlobalPosition.Y >= arena.HeightAt(orb.GlobalPosition.X, orb.GlobalPosition.Z) + orb.Radius - 0.08f, "Raised terrain cannot bury a resting sphere");
        arena.FlattenTerrain(Vector3.Zero, 100000, -4);
        arena.WaterHeight = 0;
        await Frames(120);
        Check(Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.08f, "Mana remains touching water surface above submerged ground");
        orb.ManaAmount = 2000;
        await Frames(60);
        Check(orb.Radius > arena.MaxFlightClearance && Mathf.Abs(orb.GlobalPosition.Y - orb.Radius) < 0.08f,
            "Huge accumulated mana retains surface clearance rather than being buried by the flight ceiling");

        await ClearActors();
        arena = Add(new HeightmapArena { CellsPerSide = 24, CellSize = 2, HeightScale = 0, WaterHeight = -20 }, Vector3.Zero);
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        arena.RaiseTerrain(Vector3.Zero, 10, 5);
        orb = Add(new ManaPickup(), new Vector3(4, 10, 0));
        await Frames(180);
        Check(orb.GlobalPosition.X > 5, $"Mana rolls down the actual heightmap hill (position {orb.GlobalPosition}, velocity {orb.LinearVelocity})");
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        orb.SpillAt(new Vector3(0, orb.Radius, 0));
        arena.ApplyCrater(Vector3.Zero, 6, 3);
        await Frames(150);
        Check(orb.GlobalPosition.Y < -1, "Lowered terrain lets mana fall into a crater");
    }

    private int AvailableCount()
    {
        int count = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
            if (node is ManaPickup orb && orb.IsAvailable) count++;
        return count;
    }

    private async Task CheckMergeGrowth()
    {
        AddFloor();
        var probe = new ManaPickup();
        float initialRadius = probe.Radius;
        probe.Free();
        var first = Add(new ManaPickup(), new Vector3(-initialRadius, initialRadius, 0));
        Add(new ManaPickup(), new Vector3(initialRadius, initialRadius, 0));
        var tracker = Add(new WorldManaTracker(), Vector3.Zero);
        int mergeFrames = 0;
        for (; mergeFrames < 150 && !first.IsGrowing; mergeFrames++) await Frames(1);
        tracker.Refresh();
        Check(first.IsGrowing && first.ManaAmount == 20 && AvailableCount() == 1 && tracker.TotalMana == 20,
            "Merge commits exact mana once while starting a growth animation");
        Check(mergeFrames < 120, "Supported touching balls start merging in under two seconds");
        Check(first.Radius < first.TargetRadius && first.Radius <= initialRadius + 0.02f,
            "Merged globe begins near its old radius instead of snapping to its target");
        await Frames(20);
        Check(first.IsGrowing && first.Radius > initialRadius && first.Radius < first.TargetRadius,
            "Merge radius interpolates through intermediate sizes");
        var shape = (SphereShape3D)first.GetChild<CollisionShape3D>(0).Shape;
        var visual = first.GetChild<MeshInstance3D>(1);
        var mesh = (SphereMesh)visual.Mesh;
        Check(Mathf.IsEqualApprox(shape.Radius, mesh.Radius * visual.Scale.X) && Mathf.IsEqualApprox(shape.Radius, first.Radius),
            "Visible sphere and collider grow together");
        Check(Mathf.Abs(first.GlobalPosition.Y - first.Radius) < 0.06f,
            "Growing globe remains grounded instead of lifting suddenly or sinking");
        await Frames(60);
        tracker.Refresh();
        Check(!first.IsGrowing && Mathf.IsEqualApprox(first.Radius, first.TargetRadius) && tracker.TotalMana == 20,
            "Growth completes at the exact target with unchanged resource accounting");
        first.TakeMana(10);
        Check(Mathf.IsEqualApprox(first.Radius, initialRadius), "Partial transfers still immediately resize to the remaining mana");
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}
