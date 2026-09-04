using UnityEngine;

public abstract class HardCrowdControl : StatusEffect
{
    // override to opt this specific hard CC into a per-target internal cooldown: once applied,
    // this exact effect type can't land on the same target again until duration + this many
    // seconds have passed since it was applied (see Entity.SetHardCCInternalCooldown/
    // IsHardCCOnInternalCooldown). currently only FreezeEffect opts in. skill-sourced hard CCs
    // (e.g. Waterlily's Bubble Prison) should leave this at 0 so their own, usually much longer,
    // skill cooldown remains the only gate - deliberately opt-in, not automatic for every subclass
    public virtual float InternalCooldownAfterExpiry => 0f;

    public HardCrowdControl(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        float finalDuration = duration;
        if (source != null)
            finalDuration = duration * (1 + source.immobilizeDurationMultiplier) + source.immobilizeDurationAdder;
        if (target != null)
            finalDuration *= (1 - target.tenacity);
        this.duration = finalDuration;
    }

    public override void OnApply()
    {
        if (InternalCooldownAfterExpiry > 0f && target != null)
            target.SetHardCCInternalCooldown(GetType(), duration + InternalCooldownAfterExpiry);
        Debug.Log("Hard cc applied");
    }

    public override void OnExpire()
    {
        Debug.Log("Hard cc expired");
    }
}
