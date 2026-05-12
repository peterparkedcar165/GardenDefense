using UnityEngine;

public abstract class HardCrowdControl : StatusEffect
{
    public HardCrowdControl(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        float finalDuration = duration;
        if (source != null)
            finalDuration = duration * (1 + source.immobilizeDurationMultiplier) + source.immobilizeDuration;
        if (target != null)
            finalDuration *= (1 - target.tenacity);
        this.duration = finalDuration;
    }

    public override void OnApply()
    {
        // nothing special applies here neither
        Debug.Log("Hard cc applied");
    }

    public override void OnExpire()
    {
        Debug.Log("Hard cc expired");
    }
}
