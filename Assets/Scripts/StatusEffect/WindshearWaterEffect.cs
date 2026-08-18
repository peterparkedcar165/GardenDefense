public class WindshearWaterEffect : WindshearedEffect
{
    public WindshearWaterEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source) { }

    protected override ElementalType ShredElement => ElementalType.Water;
    protected override void Adjust(float amount) => target.waterResistanceAdder += amount;
}
