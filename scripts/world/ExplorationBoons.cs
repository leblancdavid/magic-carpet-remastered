using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public sealed record BoonDefinition(string Id, string Name, string Effect);
public sealed record BoonOffer(long Id, IReadOnlyList<BoonDefinition> Choices, int SkipAntimatter);

// Match-owned ship state: respawning the pilot never replaces this inventory.
public sealed class ExplorationBoons
{
    public static readonly IReadOnlyList<BoonDefinition> Catalog = Array.AsReadOnly(new[]
    {
        new BoonDefinition("speed", "Vector drive", "+10% normal flight, booster and Dash speed"),
        new BoonDefinition("recharge", "Flux recovery", "+10% energy recharge (no antimatter income)"),
        new BoonDefinition("fire-rate", "Rapid cycling", "+10% Plasma Bolt fire rate"),
        new BoonDefinition("projectile-speed", "Accelerated plasma", "+10% Plasma Bolt projectile speed"),
        new BoonDefinition("energy-damage", "Energy focus", "+10% Plasma Bolt and Guardian energy damage"),
        new BoonDefinition("explosive-damage", "Blast focus", "+10% Pulse Burst explosive damage"),
        new BoonDefinition("deployable-damage", "Drone coordination", "+10% summoned Guardian damage (multiplies energy focus)"),
        new BoonDefinition("ship-hp", "Reinforced hull", "+20 maximum ship HP; does not heal on acquisition")
    });
    public const int MaximumStacks = 10;
    private readonly Dictionary<string, int> _stacks = new();
    private readonly List<BoonOffer> _pending = new();
    private long _nextId;
    public IReadOnlyList<BoonOffer> Pending => _pending.AsReadOnly();
    public int Stacks(string id) => _stacks.GetValueOrDefault(id);
    public float Multiplier(string id) => 1 + 0.1f * Stacks(id);
    public string Summary => string.Join("\n", Catalog.Where(b => Stacks(b.Id) > 0).Select(b =>
        $"{b.Name} ×{Stacks(b.Id)} · {(b.Id == "ship-hp" ? $"+{20 * Stacks(b.Id)} max HP" : $"+{10 * Stacks(b.Id)}%")}"));

    public BoonOffer SecureOffer(RandomNumberGenerator random, int skipAntimatter)
    {
        // Equal weights, no duplicates, capped options remain eligible for explicit skip.
        var candidates = Catalog.ToList();
        var choices = new List<BoonDefinition>();
        for (int i = 0; i < 3; i++)
        {
            int index = random.RandiRange(0, candidates.Count - 1);
            choices.Add(candidates[index]); candidates.RemoveAt(index);
        }
        var offer = new BoonOffer(++_nextId, choices.AsReadOnly(), Math.Max(0, skipAntimatter));
        _pending.Add(offer);
        return offer;
    }

    public bool Choose(long offerId, string boonId)
    {
        var offer = _pending.FirstOrDefault(o => o.Id == offerId);
        if (offer == null || !offer.Choices.Any(b => b.Id == boonId) || Stacks(boonId) >= MaximumStacks) return false;
        _pending.Remove(offer);
        _stacks[boonId] = Stacks(boonId) + 1;
        return true;
    }

    public bool Skip(long offerId, FactionBase home)
    {
        var offer = _pending.FirstOrDefault(o => o.Id == offerId);
        if (offer == null || home.IsDestroyed || home.IsQueuedForDeletion()) return false;
        _pending.Remove(offer);
        AwardAntimatter(home, offer.SkipAntimatter);
        return true;
    }

    public static void AwardAntimatter(FactionBase home, int amount)
    {
        int overflow = amount - home.DepositAntimatter(amount);
        AntimatterScatter.Spawn(home.GetParent(), home.GlobalPosition, overflow,
            home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy,
            minimumRadius: 6, name: "ExplorationBonusSpill");
    }
}
