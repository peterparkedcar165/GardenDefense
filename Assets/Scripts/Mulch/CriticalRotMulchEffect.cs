using UnityEngine;

// a reference counted DotCanCrit token on the plant plus damage over time crit chance (read by
// Entity.Damage for DoT tagged hits)
public class CriticalRotMulchEffect : MulchEffect
{
    private readonly CriticalRotMulchData typedData;
    private float applied;
    private bool on;
    // its plant's damage over time crits this level and the damage they added
    private int crits;
    private float extra;

    public CriticalRotMulchEffect(CriticalRotMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null || on) return;
        on = true;
        Entity.OnEntityHit += HandleHit;
        applied = V(typedData.dotCritChance);
        Owner.AddDotCanCrit();
        Owner.criticalDotChanceAdder += applied;
    }

    public override void Deactivate()
    {
        if (!on) return;
        on = false;
        // always let go of the static event, even when the plant is already gone
        Entity.OnEntityHit -= HandleHit;
        if (Owner != null)
        {
            Owner.RemoveDotCanCrit();
            Owner.criticalDotChanceAdder -= applied;
        }
        applied = 0f;
    }

    public override string GetStatus() => $"Damage over time crits: {crits} (+{extra:0} damage)";

    // a crit multiplies the hit by the plant's Crit Damage, so what it added is the rest
    private void HandleHit(EntityEventData hit)
    {
        if (Owner == null || hit.source != Owner || !hit.isCrit || !Entity.HasTag(hit.tags, DamageTag.DoT)) return;
        crits++;
        extra += hit.damage - hit.damage / Mathf.Max(1f, Owner.criticalDamage);
    }
}
