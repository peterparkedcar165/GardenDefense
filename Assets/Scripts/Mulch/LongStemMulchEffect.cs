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

    // its plant's range now, and without it (Entity.UpdateStats with its part taken out)
    public override string GetStatus()
    {
        if (Owner == null) return null;
        float without = Owner.attackRange - Owner.baseAttackRange * applied * Owner.attackRangeTotalMultiplier;
        return $"Attack Range: {Owner.attackRange:0.0#} ({without:0.0#} without it)";
    }
}
