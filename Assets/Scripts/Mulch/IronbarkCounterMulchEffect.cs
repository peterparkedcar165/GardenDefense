// Ironclad Riposte (class name kept from when it was an ironbark family mulch): counter damage on
// its plant, and a small shield whenever that plant's counter damage lands
public class IronbarkCounterMulchEffect : MulchEffect
{
    private readonly IronbarkCounterMulchData typedData;
    private float applied;
    private bool on;

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
        Owner.ApplyEffect(new IronbarkCounterShieldEffect(Owner, Owner, hit.damage * V(typedData.shieldPercentOfDamage), typedData.shieldDuration));
    }
}
