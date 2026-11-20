public class FourLeafCloverMulchEffect : MulchEffect
{
    private readonly FourLeafCloverMulchData typedData;
    private float applied;

    public FourLeafCloverMulchEffect(FourLeafCloverMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.rareItemDropRate);
        Owner.rareItemDropRate += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.rareItemDropRate -= applied;
        applied = 0f;
    }
}
