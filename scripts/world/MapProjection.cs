using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public static class MapProjection
{
    // Center the global survey on the starting hemisphere instead of putting
    // its strategic loop at a geographic pole. UVs wrap horizontally.
    public static readonly Basis SurveyFrame = PlanetGeometry.Frame(Vector3.Up, Vector3.Forward);

    public static Vector2 Project(Vector3 up)
    {
        Vector3 local = SurveyFrame.Inverse() * up.Normalized();
        return new Vector2(Mathf.Wrap(0.5f + Mathf.Atan2(local.X, local.Y) / Mathf.Tau, 0, 1),
            0.5f + Mathf.Asin(Mathf.Clamp(local.Z, -1, 1)) / Mathf.Pi);
    }

    public static Vector3 Unproject(Vector2 uv)
    {
        float longitude = (uv.X - 0.5f) * Mathf.Tau;
        float latitude = (uv.Y - 0.5f) * Mathf.Pi;
        float cosine = Mathf.Cos(latitude);
        return (SurveyFrame * new Vector3(Mathf.Sin(longitude) * cosine, Mathf.Cos(longitude) * cosine, Mathf.Sin(latitude))).Normalized();
    }

    public static Vector2 LocalOffset(Vector3 up, Basis playerFrame, float planetRadius)
    {
        Vector3 local = playerFrame.Inverse() * up.Normalized();
        float distance = Mathf.Atan2(new Vector2(local.X, local.Z).Length(), Mathf.Clamp(local.Y, -1, 1)) * planetRadius;
        Vector2 direction = new(local.X, local.Z);
        if (direction.LengthSquared() < 0.000001f) direction = Vector2.Right;
        return direction.Normalized() * distance;
    }

    public static Vector2 WrappedDifference(Vector2 to, Vector2 from)
    {
        Vector2 difference = to - from;
        difference.X = Mathf.Wrap(difference.X, -0.5f, 0.5f);
        return difference;
    }
}
