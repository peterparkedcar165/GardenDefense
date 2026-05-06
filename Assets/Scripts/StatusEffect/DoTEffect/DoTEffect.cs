using UnityEngine;

public abstract class DoTEffect : StatusEffect
{
    protected float tickTimer = 0f;
    protected float tickInterval;
    protected float damagePerSecond;
    public DoTEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        if (target.HasEffect<SludgeEffect>())
        {
            duration += 4f;
            target.RemoveEffect<SludgeEffect>();
        }
    }
}
