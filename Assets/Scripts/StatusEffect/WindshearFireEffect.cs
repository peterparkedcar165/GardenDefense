public class WindshearFireEffect : WindshearedEffect
{
    public WindshearFireEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Fire;
    protected override void Adjust(float amount) => target.fireResistanceAdder += amount;
}
