public class WindshearIceEffect : WindshearedEffect
{
    public WindshearIceEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Ice;
    protected override void Adjust(float amount) => target.iceResistanceAdder += amount;
}
