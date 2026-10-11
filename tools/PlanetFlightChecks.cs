using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class PlanetFlightChecks : Node
{
    private int _assertions;
    private void Check(bool valid, string message)
    {
        _assertions++;
        if (!valid) throw new System.Exception(message);
    }

    public override async void _Ready()
    {
        try
        {
            Vector3 center = new Vector3(42, -17, 88);
            foreach (int radius in PlanetSizes.Radii)
            {
                Vector3 up = Vector3.Up;
                Vector3 forward = Vector3.Forward;
                Vector3 velocity = forward * 24;
                int steps = 4096;
                float dt = Mathf.Tau * (radius + 6) / 24 / steps;
                for (int i = 0; i < steps; i++)
                {
                    Quaternion rotation = PlanetGeometry.TravelRotation(up, velocity, radius + 6, dt);
                    up = (rotation * up).Normalized();
                    forward = (rotation * forward).Normalized();
                    velocity = rotation * velocity;
                    Basis frame = PlanetGeometry.Frame(up, forward);
                    if (i % 128 == 0)
                    {
                        Check(Mathf.Abs(up.Dot(forward)) < 0.0001f, "Heading drifted out of tangent plane");
                        Check(frame.Y.DistanceTo(up) < 0.0001f && Mathf.Abs(frame.Determinant() - 1) < 0.0001f, "Invalid upright frame");
                        Vector3 position = center + up * (radius + 6);
                        Check(PlanetGeometry.Gravity(position, center, 9.8f).Normalized().Dot(-up) > 0.9999f, "Gravity must point inward on both hemispheres");
                    }
                }
                Check(up.DistanceTo(Vector3.Up) < 0.001f && forward.DistanceTo(Vector3.Forward) < 0.001f, "Full lap failed to preserve heading through poles");
                Check(Mathf.Abs(PlanetGeometry.SurfaceDistance(center + Vector3.Up * radius, center + Vector3.Down * radius, center, radius) - Mathf.Pi * radius) < 0.01f, "Antipodal surface distance incorrect");
                Check(PlanetGeometry.TravelRotation(up, Vector3.Zero, radius, 1).IsEqualApprox(Quaternion.Identity), "Idle travel must be stable");
            }
            PlanetFlightTest lab = new PlanetFlightTest();
            AddChild(lab);
            PlanetFlightController player = null!;
            foreach (Node child in lab.GetChildren())
                if (child is PlanetFlightController flight) player = flight;
            Check(player != null, "Lab must create a flight controller");
            // Exercise the actual controller through a full lap, including poles,
            // rather than relying only on isolated geometry calculations.
            player!.SetPhysicsProcess(false);
            for (int i = 0; i < 120; i++) player.SimulateFlight(1f / 60, new Vector2(0, -1), 0);
            float startTravel = player.SurfaceTravel;
            Vector3 startUp = player.LocalUp;
            Vector3 startForward = -player.GlobalBasis.Z;
            int lapSteps = 8192;
            float lapDelta = Mathf.Tau * (player.Radius + player.Clearance) / player.MoveSpeed / lapSteps;
            for (int i = 0; i < lapSteps; i++)
            {
                player.SimulateFlight(lapDelta, new Vector2(0, -1), 0);
                if (i % 256 == 0)
                {
                    Check(Mathf.Abs(player.GlobalPosition.Length() - player.Radius - player.Clearance) < 0.001f, "Controller left spherical shell");
                    Check(player.GlobalBasis.Y.Dot(player.LocalUp) > 0.9999f, "Controller lost radial upright orientation");
                }
            }
            Check(player.LocalUp.DistanceTo(startUp) < 0.002f && (-player.GlobalBasis.Z).DistanceTo(startForward) < 0.002f, "Actual flight failed full-lap heading preservation");
            Check(Mathf.Abs(player.SurfaceTravel - startTravel - Mathf.Tau * player.Radius) < 2, $"Surface odometer must track arc length: actual {player.SurfaceTravel - startTravel}, expected {Mathf.Tau * player.Radius}, speed {player.Speed}, mouse {Input.MouseMode}");
            player.ResetFlight();
            player.SetPhysicsProcess(true);
            player!.TargetClearance = 12;
            for (int i = 0; i < 180; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(Mathf.Abs(player.Clearance - 12) < 0.1f, "Lift must reach the flight ceiling");
            player.TargetClearance = 2;
            for (int i = 0; i < 180; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(Mathf.Abs(player.Clearance - 2) < 0.1f, "Radial descent must reach minimum clearance");
            Check(player.GlobalBasis.Y.Dot(player.LocalUp) > 0.9999f, "Ship must remain upright");
            player.Radius = 1200;
            player.ResetFlight();
            Check(Mathf.Abs(player.GlobalPosition.Length() - 1202) < 0.01f, "Radius change/reset must preserve requested clearance");
            player.Radius = lab.PlanetRadius;
            player.TargetClearance = 6;
            player.ResetFlight();
            foreach (string argument in OS.GetCmdlineUserArgs())
            {
                if (!argument.StartsWith("--capture=")) continue;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Error saved = GetViewport().GetTexture().GetImage().SavePng(argument.Substring("--capture=".Length));
                Check(saved == Error.Ok, "Failed to save rendered flight lab capture");
            }
            GD.Print($"Planet flight checks passed: {_assertions} assertions.");
            GetTree().Quit();
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }
}
