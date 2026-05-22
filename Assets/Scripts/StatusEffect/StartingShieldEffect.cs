public class StartingShieldEffect : ShieldEffect
{
    public override bool IsInfinite => true;

    public StartingShieldEffect(Entity target, Entity source, float amount)
        : base(target, float.MaxValue, 1, source, amount) { }
}
