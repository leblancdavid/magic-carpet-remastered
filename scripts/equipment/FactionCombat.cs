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
            _ => false
        };
        return alive && (OwnershipOf(target) == AntimatterOwnership.Neutral || OwnershipOf(target) != attacker);
    }

    public static void Damage(Node3D target, int damage, Node3D? attacker)
    {
        switch (target)
        {
            case ShipFlightController ship: ship.ApplyDamage(damage, attacker); break;
            case EnemyEcosystemEnemy enemy: enemy.ApplyDamage(damage, attacker); break;
            case SimpleMonster monster: monster.ApplyDamage(damage); break;
            case FactionBase hub: hub.ApplyDamage(damage); break;
            case CollectorDrone drone: drone.ApplyDamage(damage); break;
        }
    }
}
