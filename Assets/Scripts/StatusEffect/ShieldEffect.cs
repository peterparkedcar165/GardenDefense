public abstract class ShieldEffect : StatusEffect
{
    public float amount;
    public readonly float originalAmount;
    public virtual bool IsInfinite => false;

    protected ShieldEffect(Entity target, float duration, int level, Entity source, float amount)
        : base(target, duration, level, source)
    {
        // shields scale with the source's Heals & Shield Bonus and the target's Heals & Shield Received, like heals
        float bonus = source != null ? source.healingBonus : 0f;
        float scaled = amount * (1f + target.healingReceived) * (1f + bonus);
        this.amount = scaled;
        this.originalAmount = scaled;
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
            target.armorAdder += 150f;
            ((Insect)target).movementSpeedAdder -= 0.2f;
        }
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        // revert only when the last shield is gone (this one is still counted here)
        if (target is Snail && ShieldCount() <= 1)
        {
            target.armorAdder -= 150f;
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
