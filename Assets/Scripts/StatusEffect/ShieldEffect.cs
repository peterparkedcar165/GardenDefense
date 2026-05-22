public abstract class ShieldEffect : StatusEffect
{
    public float amount;
    public virtual bool IsInfinite => false;

    protected ShieldEffect(Entity target, float duration, int level, Entity source, float amount)
        : base(target, duration, level, source)
    {
        this.amount = amount;
        effectType = Type.positive;
    }

    public override void OnApply()  { }
    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
