using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

// Suspension and radial motion only. The ship owns input, energy and the collision solver.
public sealed class ShipHoverMotion
{
    public float HoverHeight { get; set; } = 2;
    public float Gravity { get; set; } = 18;
    public float JumpHeight { get; set; } = 4;
    public float MaximumSlopeDegrees { get; set; } = 40;
    public float MaximumClimbSpeed { get; set; } = 10;
    public float AirControl { get; set; } = 0.35f;
    public bool Supported { get; private set; }
    private float _edgeGrace;
    private float _launchLock;

    public void Reset(HeightmapArena? arena, Vector3 position)
    {
        Supported = arena != null && arena.Clearance(position) <= HoverHeight + 0.5f;
        _edgeGrace = Supported ? 0.1f : 0;
        _launchLock = 0;
    }

    public bool CanJump => _launchLock <= 0 && (Supported || _edgeGrace > 0);

    public Vector3 Launch(Vector3 velocity, Vector3 up)
    {
        LeaveSupport();
        return velocity + up * (Mathf.Max(0, velocity.Dot(up))
            + Mathf.Sqrt(2 * Gravity * JumpHeight) - velocity.Dot(up));
    }

    public void LeaveSupport()
    {
        Supported = false;
        _edgeGrace = 0;
        _launchLock = 0.15f;
    }

    public Vector3 Step(HeightmapArena arena, Vector3 position, Vector3 velocity,
        Vector3 desiredHorizontal, float acceleration, float drag, bool boosting, float delta)
    {
        Vector3 up = arena.UpAt(position);
        Vector3 horizontal = arena.Tangent(velocity, position);
        float vertical = velocity.Dot(up);
        float clearance = arena.Clearance(position);
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        const float probe = 0.75f;
        float Slope(Vector3 direction) => (arena.SurfaceHeight(position + direction * probe)
            - arena.SurfaceHeight(position - direction * probe)) / (2 * probe);
        Vector3 gradient = frame.X * Slope(frame.X) + frame.Z * Slope(frame.Z);
        float surfaceVelocity = gradient.Dot(horizontal);
        _launchLock = Mathf.Max(0, _launchLock - delta);
        Supported = _launchLock <= 0 && clearance <= HoverHeight + 0.8f
            && vertical <= surfaceVelocity + 2 && gradient.Length() <= Mathf.Tan(Mathf.DegToRad(MaximumSlopeDegrees));
        _edgeGrace = Supported ? 0.1f : Mathf.Max(0, _edgeGrace - delta);

        // Only prevent uphill travel near the surface. An airborne jump may clear
        // a small lip; descending steep terrain releases support instead of snapping down.
        if (clearance <= HoverHeight + 1.5f && gradient.Length() > Mathf.Tan(Mathf.DegToRad(MaximumSlopeDegrees)))
        {
            Vector3 uphill = gradient.Normalized();
            desiredHorizontal -= uphill * Mathf.Max(0, desiredHorizontal.Dot(uphill));
            horizontal -= uphill * Mathf.Max(0, horizontal.Dot(uphill));
        }
        float climb = gradient.Dot(desiredHorizontal);
        if (climb > MaximumClimbSpeed) desiredHorizontal *= MaximumClimbSpeed / climb;
        float rate = desiredHorizontal.LengthSquared() > 0.001f ? acceleration : drag;
        if (!Supported) rate *= desiredHorizontal.LengthSquared() > 0.001f ? AirControl : 0.1f;
        horizontal = boosting ? desiredHorizontal : horizontal.MoveToward(desiredHorizontal, rate * delta);
        surfaceVelocity = Mathf.Clamp(gradient.Dot(horizontal), -MaximumClimbSpeed, MaximumClimbSpeed);
        float radialAcceleration = -Gravity;
        if (Supported)
        {
            // Critically damped spring with gravity compensation. Downward force
            // cannot exceed gravity, so a falling surface does not pull the ship down.
            radialAcceleration = Mathf.Clamp((HoverHeight - clearance) * 100
                + (surfaceVelocity - vertical) * 20, -Gravity, 80);
        }
        vertical += radialAcceleration * delta;
        return horizontal + up * vertical;
    }
}
