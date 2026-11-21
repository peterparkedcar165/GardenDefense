// percent attack range on its plant (StatType.AttackRange adds to attackRangeMultiplier, a share of the base range)
public class LongStemMulchEffect : MulchEffect
{
    private readonly LongStemMulchData typedData;
    private float applied;

    public LongStemMulchEffect(LongStemMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.range);
        PlantStatApplier.Apply(Owner, StatType.AttackRange, applied);
    }

    public override void Deactivate()
    {
        if (Owner != null) PlantStatApplier.Apply(Owner, StatType.AttackRange, -applied);
        applied = 0f;
    }
}
