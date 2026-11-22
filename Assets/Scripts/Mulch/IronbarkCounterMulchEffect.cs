using UnityEngine;

// Ironclad Riposte (class name kept from when it was an ironbark family mulch): counter damage on
// its plant, and a shield whenever that plant's counter damage lands, which stacks while counter
// hits keep landing (IronbarkCounterShieldEffect)
public class IronbarkCounterMulchEffect : MulchEffect
{
    private readonly IronbarkCounterMulchData typedData;
    private float applied;
    private bool on;
    // the shield's full timer as it came out of ApplyEffect (buff duration stats included), so a
    // stack restarts it at the same length
    private float fullDuration;

    // the shield the plant's last counter hit gave, for the tooltip's number. 0 until one lands
    public float LastShieldGain { get; private set; }
    // this level: the counter damage its bonus added, and the shield its counter hits gave
    private float extraDamage;
    private float shieldGained;

    public IronbarkCounterMulchEffect(IronbarkCounterMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || on) return;
        on = true;
        applied = V(typedData.counterDamageBonus);
        PlantStatApplier.Apply(Owner, StatType.CounterDamage, applied);
        Entity.OnEntityHit += HandleHit;
    }

    public override void Deactivate()
    {
        if (!on) return;
        on = false;
        Entity.OnEntityHit -= HandleHit;
        if (Owner != null) PlantStatApplier.Apply(Owner, StatType.CounterDamage, -applied);
        applied = 0f;
    }

    private void HandleHit(EntityEventData hit)
    {
        if (!OwnerAlive || hit.source != Owner || !Entity.HasTag(hit.tags, DamageTag.Counter)) return;
        // counter hits deal 1 + Counter Damage times; its part is applied over that
        extraDamage += hit.damage * applied / Mathf.Max(0.01f, 1f + Owner.counterDamage);
        float raw = hit.damage * V(typedData.shieldPercentOfDamage);
        if (raw <= 0f) return;
        IronbarkCounterShieldEffect existing = Owner.GetEffect<IronbarkCounterShieldEffect>();
        if (existing != null && existing.source == Owner)
        {
            LastShieldGain = existing.Stack(raw, fullDuration > 0f ? fullDuration : typedData.shieldDuration);
            shieldGained += LastShieldGain;
            return;
        }
        IronbarkCounterShieldEffect shield = new IronbarkCounterShieldEffect(Owner, Owner, raw, typedData.shieldDuration);
        Owner.ApplyEffect(shield);
        fullDuration = shield.duration;
        LastShieldGain = shield.amount;
        shieldGained += LastShieldGain;
    }

    public override string GetStatus() => $"Extra counter damage: {extraDamage:0}\nShield gained: {shieldGained:0}";
}
