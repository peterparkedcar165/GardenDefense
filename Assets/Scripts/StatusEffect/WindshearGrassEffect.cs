public class WindshearGrassEffect : WindshearedEffect
{
    public WindshearGrassEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Grass;
    protected override void Adjust(float amount) => target.grassResistanceAdder += amount;
}
