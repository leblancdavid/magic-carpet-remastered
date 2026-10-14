using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public static class FactionCombat
{
    public static AntimatterOwnership OwnershipOf(Node3D actor) => actor switch
    {
        ShipFlightController ship => ship.Ownership,
        EnemyEcosystemEnemy { Role: EnemyEcosystemEnemy.EnemyRole.RivalShip } => AntimatterOwnership.Enemy,
        FactionBase hub => hub.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy,
        CollectorDrone drone => drone.UsePlayerAntimatter ? AntimatterOwnership.Player : AntimatterOwnership.Enemy,
        BaseTacticalDrone drone => drone.Ownership,
        BaseStrikeDrone drone => drone.IsPlayerOwned ? AntimatterOwnership.Player : AntimatterOwnership.Enemy,
        GuardianDrone drone => drone.Ownership,
        _ => AntimatterOwnership.Neutral
    };

    public static bool CanDamage(Node3D target, AntimatterOwnership attacker)
    {
        if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion()) return false;
        bool alive = target switch
        {
            ShipFlightController ship => ship.Health > 0,
            EnemyEcosystemEnemy enemy => enemy.Health > 0,
            SimpleMonster monster => monster.Health > 0,
            FactionBase hub => !hub.IsDestroyed,
            CollectorDrone drone => drone.Health > 0,
            BaseTacticalDrone drone => drone.Health > 0,
            BaseStrikeDrone drone => drone.Health > 0,
            GuardianDrone drone => drone.Health > 0,
            _ => false
        };
        return alive && (OwnershipOf(target) == AntimatterOwnership.Neutral || OwnershipOf(target) != attacker);
    }

    public static void Damage(Node3D target, int damage, Node3D? attacker)
    {
        int before = HealthOf(target);
        switch (target)
        {
            case ShipFlightController ship: ship.ApplyDamage(damage, attacker); break;
            case EnemyEcosystemEnemy enemy: enemy.ApplyDamage(damage, attacker); break;
            case SimpleMonster monster: monster.ApplyDamage(damage); break;
            case FactionBase hub: hub.ApplyDamage(damage, attacker); break;
            case CollectorDrone drone: drone.ApplyDamage(damage); break;
            case BaseTacticalDrone drone: drone.ApplyDamage(damage); break;
            case BaseStrikeDrone drone: drone.ApplyDamage(damage); break;
            case GuardianDrone drone: drone.ApplyDamage(damage); break;
        }
        if (damage > 0 && before > HealthOf(target) && GodotObject.IsInstanceValid(attacker) && attacker is ShipFlightController pilot)
            pilot.RecordCombatDamage();
    }

    private static int HealthOf(Node3D target) => target switch
    {
        ShipFlightController ship => ship.Health,
        EnemyEcosystemEnemy enemy => enemy.Health,
        SimpleMonster monster => monster.Health,
        FactionBase hub => hub.Health,
        CollectorDrone drone => drone.Health,
        BaseTacticalDrone drone => drone.Health,
        BaseStrikeDrone drone => drone.Health,
        GuardianDrone drone => drone.Health,
        _ => 0
    };
}
