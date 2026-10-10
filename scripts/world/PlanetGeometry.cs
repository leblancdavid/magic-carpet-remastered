using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public static class PlanetGeometry
{
    public static Vector3 Up(Vector3 position, Vector3 center) => (position - center).Normalized();

    public static Vector3 Gravity(Vector3 position, Vector3 center, float strength) => -Up(position, center) * strength;

    public static float SurfaceDistance(Vector3 a, Vector3 b, Vector3 center, float radius)
        => Mathf.Acos(Mathf.Clamp(Up(a, center).Dot(Up(b, center)), -1, 1)) * radius;

    // Exact great-circle step. The same rotation transports heading and velocity,
    // avoiding pole singularities and an artificial compass-north preference.
    public static Quaternion TravelRotation(Vector3 up, Vector3 tangentVelocity, float radialDistance, float delta)
    {
        float speed = tangentVelocity.Length();
        if (speed < 0.00001f) return Quaternion.Identity;
        return new Quaternion(up.Cross(tangentVelocity / speed).Normalized(), speed * delta / radialDistance);
    }

    public static Basis Frame(Vector3 up, Vector3 forward)
    {
        forward = (forward - up * forward.Dot(up)).Normalized();
        return new Basis(forward.Cross(up).Normalized(), up, -forward).Orthonormalized();
    }
}
