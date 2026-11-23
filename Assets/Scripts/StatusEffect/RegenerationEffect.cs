using UnityEngine;

// base class for healing-over-time effects. subclass this to create specialised regen variants.
// the timing is the shared over time rule (OverTimeTicker, Tommy, 2026-11-23): subclasses say what
// one tick heals in HealTick, this class runs the ticks and heals the part of a tick left over when
// the effect runs out on its own
public class RegenerationEffect : StatusEffect
{
    protected readonly OverTimeTicker ticks;
    // what one tick heals (the name is from when every regen ticked once a second)
    protected float healingPerSecond;
    // seconds between two ticks, after any tick speed bonus
    protected float TickInterval => ticks.Interval;

    public RegenerationEffect(Entity target, float duration, int level, Entity source, float healingPerSecond, float tickInterval = 1f)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        sourceStackable = true;
        tags = new EffectTag[] { EffectTag.Regenerative };
        this.healingPerSecond = healingPerSecond;
        ticks = new OverTimeTicker(tickInterval);
        if (source != null && source.regenerationDuration != 0f)
            this.duration *= (1f + source.regenerationDuration);
    }

    public override void OnReapply(StatusEffect previous)
    {
        if (previous is RegenerationEffect prev) ticks.CarryFrom(prev.ticks);
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(-0.4f, 0f, 0f), "Regeneration", new Color(0.4f, 1f, 0.4f));
    }

    public override void OnTick(float deltaTime)
    {
        int due = ticks.Advance(deltaTime);
        for (int i = 0; i < due; i++) HealTick(1f);
    }

    public override void OnNaturalExpire()
    {
        float share = ticks.LeftoverShare(duration);
        if (share > 0.0001f) HealTick(share);
    }

    // one tick's healing. share is 1 for a whole tick, less for the bit left at a natural expiry
    protected virtual void HealTick(float share) => HealTarget(healingPerSecond * share);

    public override string GetName() => "<color=#88FF88>Regeneration</color>";
    public override string GetDescription() => $"Recovering <color=#88FF88><b>{healingPerSecond:F0}</b></color> health per second.";
}
