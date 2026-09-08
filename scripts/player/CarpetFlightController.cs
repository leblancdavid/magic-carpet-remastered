using Godot;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class CarpetFlightController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 18.0f;
    [Export] public float VerticalSpeed { get; set; } = 10.0f;
    [Export] public float Acceleration { get; set; } = 8.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float MinimumAltitude { get; set; } = 1.5f;
    [Export] public float FireCooldownSeconds { get; set; } = 0.25f;

    public int Mana { get; private set; }
    public int Health { get; private set; } = 100;

    private Node3D _cameraPivot = null!;
    private Camera3D _camera = null!;
    private float _yaw;
    private float _pitch = -0.2f;
    private float _fireCooldown;

    public override void _Ready()
    {
        AddToGroup("player");
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _cameraPivot = new Node3D { Name = "CameraPivot" };
        AddChild(_cameraPivot);
        _cameraPivot.Position = new Vector3(0.0f, 1.2f, 0.0f);

        _camera = new Camera3D
        {
            Name = "Camera3D",
            Current = true,
            Position = new Vector3(0.0f, 2.0f, 8.0f),
            Fov = 75.0f
        };
        _cameraPivot.AddChild(_camera);
        _camera.LookAt(_cameraPivot.GlobalPosition, Vector3.Up);

        AddCarpetVisual();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.2f, 0.6f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _fireCooldown = Mathf.Max(0.0f, _fireCooldown - deltaF);

        Rotation = new Vector3(0.0f, _yaw, 0.0f);
        _cameraPivot.Rotation = new Vector3(_pitch, 0.0f, 0.0f);

        Vector3 localInput = Vector3.Zero;
        localInput.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        localInput.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
        localInput.Y = Input.GetActionStrength("ascend") - Input.GetActionStrength("descend");

        Vector3 desiredVelocity = Vector3.Zero;
        if (localInput.LengthSquared() > 0.001f)
        {
            localInput = localInput.Normalized();
            Vector3 horizontal = (GlobalBasis * new Vector3(localInput.X, 0.0f, localInput.Z)) * MoveSpeed;
            desiredVelocity = new Vector3(horizontal.X, localInput.Y * VerticalSpeed, horizontal.Z);
        }

        Velocity = Velocity.Lerp(desiredVelocity, Acceleration * deltaF);
        MoveAndSlide();

        if (GlobalPosition.Y < MinimumAltitude)
        {
            GlobalPosition = new Vector3(GlobalPosition.X, MinimumAltitude, GlobalPosition.Z);
            Velocity = new Vector3(Velocity.X, Mathf.Max(0.0f, Velocity.Y), Velocity.Z);
        }

        if (Input.IsActionPressed("cast_primary") && _fireCooldown <= 0.0f)
        {
            CastPrimarySpell();
            _fireCooldown = FireCooldownSeconds;
        }
    }

    public void AddMana(int amount)
    {
        Mana += amount;
    }

    public void ApplyDamage(int amount)
    {
        Health = Mathf.Max(0, Health - amount);
        if (Health == 0)
        {
            GetTree().ReloadCurrentScene();
        }
    }

    private void CastPrimarySpell()
    {
        var projectile = new Spells.SpellProjectile();
        GetTree().CurrentScene.AddChild(projectile);

        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        projectile.GlobalPosition = _camera.GlobalPosition + direction * 1.5f;
        projectile.Launch(direction, this);
    }

    private void AddCarpetVisual()
    {
        var carpet = new MeshInstance3D { Name = "CarpetVisual" };
        carpet.Mesh = new BoxMesh { Size = new Vector3(2.0f, 0.12f, 3.0f) };
        carpet.Position = new Vector3(0.0f, -0.35f, 0.0f);

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.38f, 0.16f, 0.82f),
            Roughness = 0.65f
        };
        carpet.MaterialOverride = material;
        AddChild(carpet);

        var collision = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = new Vector3(1.6f, 0.6f, 2.4f) }
        };
        AddChild(collision);
    }
}
