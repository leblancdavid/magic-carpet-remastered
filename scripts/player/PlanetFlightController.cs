using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class PlanetFlightController : Node3D
{
    public float Radius { get; set; } = 800;
    public float MoveSpeed { get; set; } = 24;
    public float TargetClearance { get; set; } = 6;
    public Vector3 Center { get; set; } = Vector3.Zero;
    public float Clearance { get; private set; } = 6;
    public float SurfaceTravel { get; private set; }
    public float Speed => _tangentVelocity.Length();
    public float UpDegreesPerSecond => Mathf.RadToDeg(Speed / (Radius + Clearance));
    public bool FirstPerson { get; private set; }
    public Vector3 LocalUp => PlanetGeometry.Up(GlobalPosition, Center);
    public Vector3 Gravity => PlanetGeometry.Gravity(GlobalPosition, Center, 9.8f);

    private Vector3 _up = Vector3.Up;
    private Vector3 _forward = Vector3.Forward;
    private Vector3 _tangentVelocity;
    private float _radialSpeed;
    private float _pitch;
    private float _bank;
    private float _dashTimer;
    private float _dashCooldown;
    private Node3D _visual = null!;
    private Node3D _cameraPivot = null!;
    private Camera3D _camera = null!;

    public override void _Ready()
    {
        _visual = new Node3D();
        AddChild(_visual);
        _visual.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2.6f, 0.12f, 3.6f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.12f, 0.65f) }
        });
        _cameraPivot = new Node3D();
        AddChild(_cameraPivot);
        _camera = new Camera3D { Current = true, Far = 12000, Near = 0.1f, Fov = 75 };
        _cameraPivot.AddChild(_camera);
        ResetFlight();
    }

    public void ResetFlight()
    {
        _up = Vector3.Up;
        _forward = Vector3.Forward;
        _tangentVelocity = Vector3.Zero;
        _radialSpeed = 0;
        _pitch = 0;
        _dashTimer = _dashCooldown = 0;
        Clearance = Mathf.Clamp(TargetClearance, 2, 12);
        SurfaceTravel = 0;
        UpdateTransform(1);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouse && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _forward = _forward.Rotated(_up, -mouse.Relative.X * 0.0025f);
            _pitch = Mathf.Clamp(_pitch - mouse.Relative.Y * 0.0025f, -1.1f, 1.1f);
        }
        if (@event.IsActionPressed("toggle_camera")) FirstPerson = !FirstPerson;
        if (@event.IsActionPressed("cast_mobility") && _dashCooldown <= 0 && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _dashTimer = 0.4f;
            _dashCooldown = 3;
            _tangentVelocity = _forward * 46;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        bool active = Input.MouseMode == Input.MouseModeEnum.Captured;
        Vector2 input = active ? Input.GetVector("move_left", "move_right", "move_forward", "move_back") : Vector2.Zero;
        float verticalInput = active ? Input.GetAxis("descend", "ascend") : 0;
        SimulateFlight((float)delta, input, verticalInput);
    }

    public void SimulateFlight(float dt, Vector2 input, float verticalInput)
    {
        input = input.LimitLength();
        Vector3 right = _forward.Cross(_up).Normalized();
        Vector3 desired = right * input.X - _forward * input.Y;
        float speed = _dashTimer > 0 ? 46 : MoveSpeed;
        if (_dashTimer > 0) desired = _forward;
        _tangentVelocity = _tangentVelocity.MoveToward(desired * speed, (desired.LengthSquared() > 0 ? 28 : 9) * dt);
        _dashTimer = Mathf.Max(0, _dashTimer - dt);
        _dashCooldown = Mathf.Max(0, _dashCooldown - dt);

        // Gravity is radial; carpet lift counteracts it and controls surface clearance.
        TargetClearance = Mathf.Clamp(TargetClearance + verticalInput * 11 * dt, 2, 12);
        float lift = 9.8f + (TargetClearance - Clearance) * 16 - _radialSpeed * 8;
        _radialSpeed += (Gravity.Dot(_up) + lift) * dt;
        Clearance = Mathf.Clamp(Clearance + _radialSpeed * dt, 2, 12);
        if ((Clearance <= 2 && _radialSpeed < 0) || (Clearance >= 12 && _radialSpeed > 0)) _radialSpeed = 0;

        Quaternion travel = PlanetGeometry.TravelRotation(_up, _tangentVelocity, Radius + Clearance, dt);
        _up = (travel * _up).Normalized();
        _forward = (travel * _forward).Normalized();
        _tangentVelocity = travel * _tangentVelocity;
        SurfaceTravel += Speed * dt * Radius / (Radius + Clearance);
        _bank = Mathf.Lerp(_bank, -input.X * Mathf.DegToRad(24), 1 - Mathf.Exp(-8 * dt));
        UpdateTransform(dt);
    }

    private void UpdateTransform(float delta)
    {
        GlobalTransform = new Transform3D(PlanetGeometry.Frame(_up, _forward), Center + _up * (Radius + Clearance));
        if (_camera == null) return;
        _visual.Rotation = new Vector3(0, 0, _bank);
        _visual.Visible = !FirstPerson;
        _cameraPivot.Rotation = new Vector3(_pitch, 0, 0);
        Vector3 target = FirstPerson ? new Vector3(0, 1.3f, -0.5f) : new Vector3(0, 3, 8);
        _camera.Position = _camera.Position.Lerp(target, 1 - Mathf.Exp(-10 * delta));
        // Looking upward can swing the chase camera into the planet at low altitude.
        Vector3 cameraOffset = _camera.GlobalPosition - Center;
        if (cameraOffset.Length() < Radius + 0.5f)
            _camera.GlobalPosition = Center + cameraOffset.Normalized() * (Radius + 0.5f);
    }
}
