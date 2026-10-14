using Godot;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class ShipFlightController
{
    public int ScatterPelletDamage => Mathf.RoundToInt((8 + 3 * Mathf.Max(0, GetEquipmentRank("scatter-cannon") - 1)) * Boons.Multiplier("energy-damage"));
    public int SeekerDamage => Mathf.RoundToInt((42 + 14 * Mathf.Max(0, GetEquipmentRank("seeker-missile") - 1)) * Boons.Multiplier("explosive-damage"));

    private void CastScatterEquipment()
    {
        SpendEnergy(_scatterEquipment.EnergyCost);
        Vector3 forward = AimDirection;
        Vector3 right = forward.Cross(LocalUp).Normalized();
        if (right.LengthSquared() < 0.01f) right = GlobalBasis.X;
        Vector3 up = right.Cross(forward).Normalized();
        for (int pellet = 0; pellet < 7; pellet++)
        {
            float angle = (pellet - 1) * Mathf.Tau / 6;
            Vector3 direction = pellet == 0 ? forward : (forward + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * 0.11f).Normalized();
            var shot = new WeaponProjectile { Damage = ScatterPelletDamage, Speed = 60, LifetimeSeconds = 0.6f,
                TerrainCraterRadius = 0, TerrainCraterDepth = 0 };
            GetParent().AddChild(shot);
            shot.GlobalPosition = AimOrigin + forward * 1.5f;
            shot.Launch(direction, this);
        }
        PlayEquipmentSound();
    }

    private void CastSeekerEquipment()
    {
        SpendEnergy(_seekerEquipment.EnergyCost);
        var missile = new SeekerMissile { Damage = SeekerDamage, OwnerShip = this, Ownership = Ownership };
        GetParent().AddChild(missile);
        missile.GlobalPosition = AimOrigin + AimDirection * 1.5f;
        missile.Launch(AimDirection);
        PlayEquipmentSound();
    }
}
