public class WindshearGroundEffect : WindshearedEffect
{
    public WindshearGroundEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Ground;
    protected override void Adjust(float amount) => target.groundResistanceAdder += amount;
}
