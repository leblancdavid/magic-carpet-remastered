using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class ShipFlightController
{
    public const int TeleportEnergyCost = 20;
    private float _teleportChannel, _teleportCooldown;
    private Vector3 _teleportOrigin;
    public bool IsTeleporting => _teleportChannel > 0;
    public float TeleportCooldownRemaining => _teleportCooldown;
    public int TeleportRank => HomeBase?.Construction.UpgradeRank("teleport-home", true) ?? 0;
    public string TeleportStatus => TeleportRank == 0 ? "" : IsTeleporting ? $"TELEPORT {_teleportChannel:0.0}s · hold position"
        : _teleportCooldown > 0 ? $"T · return home {_teleportCooldown:0}s" : $"T · return home · {TeleportEnergyCost} E";

    public bool TryActivateTeleport()
    {
        if (Health <= 0 || GetTree().Paused || GameplayInputBlocked || HomeBase == null || HomeBase.IsDestroyed
            || TeleportRank == 0 || _teleportCooldown > 0 || Energy < TeleportEnergyCost || IsTeleporting) return false;
        _teleportOrigin = GlobalPosition;
        _teleportChannel = 3;
        return true;
    }
    private void CancelTeleport() => _teleportChannel = 0;
    private bool AdvanceTeleport(float delta, bool moving)
    {
        if (!IsTeleporting) return false;
        bool firing = IsPlayerControlled && (Input.IsActionPressed("activate_primary") || Input.IsActionPressed("activate_claim"));
        if (moving || firing || IsBoosting || _jumpBuffer > 0 || GameplayInputBlocked || HomeBase == null || HomeBase.IsDestroyed
            || (_arena?.SurfaceDistance(GlobalPosition, _teleportOrigin) ?? new Vector2(GlobalPosition.X - _teleportOrigin.X, GlobalPosition.Z - _teleportOrigin.Z).Length()) > 1 || Energy < TeleportEnergyCost)
        { CancelTeleport(); return false; }
        _teleportChannel = Mathf.Max(0, _teleportChannel - delta);
        if (IsTeleporting) return false;
        var home = HomeBase!;
        Vector3? arrival = null;
        // Query the actual hull at several home-adjacent hover positions; never teleport inside another body.
        using var query = new PhysicsShapeQueryParameters3D
        {
            Shape = GetNode<CollisionShape3D>("CollisionShape3D").Shape,
            CollisionMask = CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.Tau / 12;
            Vector3 candidate = home.GlobalPosition + home.GlobalBasis * new Vector3(Mathf.Cos(angle) * 12, 0, Mathf.Sin(angle) * 12);
            candidate = _arena?.OnSurface(candidate, MinimumAltitude + 1) ?? candidate + Vector3.Up * (MinimumAltitude + 1);
            query.Transform = new Transform3D(_arena?.IsSpherical == true ? PlanetGeometry.Frame(_arena.UpAt(candidate), -home.GlobalBasis.Z) : Basis.Identity, candidate);
            if (GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0) { arrival = candidate; break; }
        }
        if (!arrival.HasValue) return false;
        SpendEnergy(TeleportEnergyCost);
        _teleportCooldown = 75 - 15 * TeleportRank;
        GlobalPosition = arrival.Value;
        Velocity = Vector3.Zero;
        _boosterTimer = _jumpBuffer = 0;
        _arena?.Orient(this, -home.GlobalBasis.Z);
        ConfigureHover(); _hover.Reset(_arena, GlobalPosition);
        TrySynchronizeLoadout();
        return true;
    }
}
