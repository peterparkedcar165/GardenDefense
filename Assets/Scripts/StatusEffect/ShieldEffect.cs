public abstract class ShieldEffect : StatusEffect
{
    public float amount;
    public readonly float originalAmount;
    public virtual bool IsInfinite => false;

    protected ShieldEffect(Entity target, float duration, int level, Entity source, float amount)
        : base(target, duration, level, source)
    {
        this.amount = amount;
        this.originalAmount = amount;
        effectType = Type.positive;
    }

    public override string GetDescription() =>
        $"Remaining Shield: [<color=grey><b>{amount:F0}/{originalAmount:F0}</b></color>]";

    public override void OnApply()
    {
        // a shelled snail trades speed for armor. only the first shield applies it,
        // so additional shields (of any type) don't stack the bonus
        if (target is Snail && ShieldCount() == 1)
        {
            target.physicalResistanceAdder += 0.6f;
            ((Insect)target).movementSpeedAdder -= 0.2f;
        }
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        // revert only when the last shield is gone (this one is still counted here)
        if (target is Snail && ShieldCount() <= 1)
        {
            target.physicalResistanceAdder -= 0.6f;
            ((Insect)target).movementSpeedAdder += 0.2f;
        }
    }

    private int ShieldCount()
    {
        int count = 0;
        foreach (StatusEffect e in target.activeEffects)
            if (e is ShieldEffect) count++;
        return count;
    }
}
