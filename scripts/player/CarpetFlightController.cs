using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class CarpetFlightController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 18.0f;
    [Export] public float VerticalSpeed { get; set; } = 10.0f;
    [Export] public float Acceleration { get; set; } = 8.0f;
    [Export] public float Drag { get; set; } = 4.5f;
    [Export] public float BankDegrees { get; set; } = 18.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float MinimumAltitude { get; set; } = 1.5f;
    [Export] public float GroundProbeHeight { get; set; } = 24.0f;
    [Export] public float GroundProbeDepth { get; set; } = 96.0f;
    [Export] public float FireCooldownSeconds { get; set; } = 0.25f;
    [Export] public int PrimarySpellManaCost { get; set; } = 5;

    public int Mana { get; private set; } = 40;
    public int Health { get; private set; } = 100;
    public string CameraMode => _firstPersonCamera ? "First Person" : "Chase";
    public string StatusMessage { get; private set; } = "Collect mana and destroy the red monsters.";

    private Node3D _cameraPivot = null!;
    private Camera3D _camera = null!;
    private MeshInstance3D _carpetVisual = null!;
    private StandardMaterial3D _carpetMaterial = null!;
    private HeightmapArena? _arena;
    private float _yaw;
    private float _pitch = -0.2f;
    private float _fireCooldown;
    private float _feedbackTimer;
    private bool _firstPersonCamera;

    public override void _Ready()
    {
        AddToGroup("player");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;

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

        if (@event.IsActionPressed("toggle_camera"))
        {
            _firstPersonCamera = !_firstPersonCamera;
            StatusMessage = $"Camera: {CameraMode}";
            _feedbackTimer = 1.2f;
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
        _feedbackTimer = Mathf.Max(0.0f, _feedbackTimer - deltaF);

        Rotation = new Vector3(0.0f, _yaw, 0.0f);
        _cameraPivot.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
        UpdateCamera(deltaF);

        Vector3 localInput = Vector3.Zero;
        localInput.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        localInput.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
        localInput.Y = Input.GetActionStrength("ascend") - Input.GetActionStrength("descend");

        Vector3 desiredVelocity = Velocity.MoveToward(Vector3.Zero, Drag * deltaF);
        if (localInput.LengthSquared() > 0.001f)
        {
            localInput = localInput.Normalized();
            Vector3 horizontal = (GlobalBasis * new Vector3(localInput.X, 0.0f, localInput.Z)) * MoveSpeed;
            desiredVelocity = new Vector3(horizontal.X, localInput.Y * VerticalSpeed, horizontal.Z);
        }

        Velocity = Velocity.Lerp(desiredVelocity, Acceleration * deltaF);
        UpdateCarpetBank(localInput, deltaF);
        MoveAndSlide();

        ClampAboveGround();

        if (Input.IsActionPressed("cast_primary") && _fireCooldown <= 0.0f)
        {
            _fireCooldown = FireCooldownSeconds;
            CastPrimarySpell();
        }
    }

    public void AddMana(int amount)
    {
        Mana += amount;
        StatusMessage = $"Mana +{amount}";
        _feedbackTimer = 1.0f;
    }

    public void ApplyDamage(int amount)
    {
        Health = Mathf.Max(0, Health - amount);
        StatusMessage = $"Hit for {amount}";
        _feedbackTimer = 0.35f;
        _carpetMaterial.AlbedoColor = new Color(1.0f, 0.25f, 0.2f);

        if (Health == 0)
        {
            GetTree().ReloadCurrentScene();
        }
    }

    private void CastPrimarySpell()
    {
        if (Mana < PrimarySpellManaCost)
        {
            StatusMessage = "Not enough mana.";
            _feedbackTimer = 0.8f;
            return;
        }

        Mana -= PrimarySpellManaCost;
        StatusMessage = "Firebolt";
        _feedbackTimer = 0.35f;

        var projectile = new Spells.SpellProjectile();
        GetTree().CurrentScene.AddChild(projectile);

        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        projectile.GlobalPosition = _camera.GlobalPosition + direction * 1.5f;
        projectile.Launch(direction, this);
    }

    private void ClampAboveGround()
    {
        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;

        if (_arena != null)
        {
            float terrainMinimumY = _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z) + MinimumAltitude;
            if (GlobalPosition.Y < terrainMinimumY)
            {
                GlobalPosition = new Vector3(GlobalPosition.X, terrainMinimumY, GlobalPosition.Z);
                Velocity = new Vector3(Velocity.X, Mathf.Max(0.0f, Velocity.Y), Velocity.Z);
            }

            return;
        }

        Vector3 origin = GlobalPosition + Vector3.Up * GroundProbeHeight;
        Vector3 target = GlobalPosition + Vector3.Down * GroundProbeDepth;
        var query = PhysicsRayQueryParameters3D.Create(origin, target);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0 || !result.ContainsKey("position"))
        {
            return;
        }

        float minimumY = ((Vector3)result["position"]).Y + MinimumAltitude;
        if (GlobalPosition.Y >= minimumY)
        {
            return;
        }

        GlobalPosition = new Vector3(GlobalPosition.X, minimumY, GlobalPosition.Z);
        Velocity = new Vector3(Velocity.X, Mathf.Max(0.0f, Velocity.Y), Velocity.Z);
    }

    private void UpdateCamera(float delta)
    {
        Vector3 targetPosition = _firstPersonCamera
            ? new Vector3(0.0f, 0.25f, -0.45f)
            : new Vector3(0.0f, 2.0f, 8.0f);

        _camera.Position = _camera.Position.Lerp(targetPosition, 12.0f * delta);
        if (_firstPersonCamera)
        {
            _camera.Rotation = Vector3.Zero;
        }
        else
        {
            _camera.LookAt(_cameraPivot.GlobalPosition, Vector3.Up);
        }

        if (_feedbackTimer <= 0.0f)
        {
            _carpetMaterial.AlbedoColor = _carpetMaterial.AlbedoColor.Lerp(new Color(0.38f, 0.16f, 0.82f), 10.0f * delta);
        }
    }

    private void UpdateCarpetBank(Vector3 localInput, float delta)
    {
        float targetRoll = Mathf.DegToRad(-localInput.X * BankDegrees);
        float targetPitch = Mathf.DegToRad(localInput.Z * 7.0f);
        _carpetVisual.Rotation = new Vector3(
            Mathf.LerpAngle(_carpetVisual.Rotation.X, targetPitch, 8.0f * delta),
            0.0f,
            Mathf.LerpAngle(_carpetVisual.Rotation.Z, targetRoll, 8.0f * delta));
    }

    private void AddCarpetVisual()
    {
        _carpetVisual = new MeshInstance3D { Name = "CarpetVisual" };
        _carpetVisual.Mesh = new BoxMesh { Size = new Vector3(2.0f, 0.12f, 3.0f) };
        _carpetVisual.Position = new Vector3(0.0f, -0.35f, 0.0f);

        _carpetMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.38f, 0.16f, 0.82f),
            Roughness = 0.65f
        };
        _carpetVisual.MaterialOverride = _carpetMaterial;
        AddChild(_carpetVisual);

        var collision = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = new Vector3(1.6f, 0.6f, 2.4f) }
        };
        AddChild(collision);
    }
}
