using UnityEngine;

// every damage over time status effect. the timing is the shared rule (OverTimeTicker, Tommy,
// 2026-11-23): each subclass sets its amount when it lands and says what one tick deals in
// DealTick; this class runs the ticks, a faster tick speed included, and pays the part of a tick
// left over when the effect runs out on its own
public abstract class DoTEffect : StatusEffect
{
    // only ever replaced by an echo seed copy's own clone (OnCopiedForEcho)
    protected OverTimeTicker ticks;
    protected float damagePerSecond;
    // set when something shortens it to nothing on purpose (Aloe Vera cutting a Scorch): that is a
    // cleanse, so no leftover tick is paid when it then runs out
    protected bool cleansed;

    public DoTEffect(Entity target, float duration, int level, Entity source, float baseInterval = 1f) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        ticks = new OverTimeTicker(baseInterval);
        if (source != null) this.duration *= Mathf.Max(0f, 1f + source.dotDuration);
    }

    // seconds between two ticks, after any tick speed bonus
    public float TickInterval => ticks.Interval;

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is DoTEffect prev) ticks.CarryFrom(prev.ticks);
    }

    // an echo seed copy ticks on its own timer, not the original's
    protected override void OnCopiedForEcho() => ticks = ticks.Clone();

    public override void OnApply()
    {
        SludgeEffect sludge = target.GetEffect<SludgeEffect>();
        if (sludge != null)
        {
            duration += sludge.bonusDuration;
            target.RemoveEffect<SludgeEffect>();
            // Botanical Alchemy: the Sludge was used up, which pays the plant that applied it
            MulchManager.ReactionEvent(sludge, sludge.source, target.transform.position);
        }
    }

    public override void OnTick(float deltaTime)
    {
        int due = ticks.Advance(deltaTime);
        for (int i = 0; i < due && TargetStanding; i++) DealTick(1f);
    }

    public override void OnNaturalExpire()
    {
        if (cleansed) return;
        float share = ticks.LeftoverShare(duration);
        if (share > 0.0001f && TargetStanding) DealTick(share);
    }

    protected bool TargetStanding => target != null && target.health > 0f;

    // a whole tick, not the leftover share at a natural expiry: only whole ticks pay Botanical
    // Alchemy (proposal, 2026-11-23: a 6 s Burn pays 1 + 6 Sun at Common, whatever its tick speed)
    protected static bool IsWholeTick(float share) => share >= 0.999f;

    // one tick's damage. share is 1 for a whole tick, less for the bit left at a natural expiry
    protected abstract void DealTick(float share);
}
