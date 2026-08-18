public class WindshearPoisonEffect : WindshearedEffect
{
    public WindshearPoisonEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Poison;
    protected override void Adjust(float amount) => target.poisonResistanceAdder += amount;
}
