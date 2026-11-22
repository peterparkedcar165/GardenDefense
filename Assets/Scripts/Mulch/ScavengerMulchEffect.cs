public class ScavengerMulchEffect : MulchEffect
{
    private readonly ScavengerMulchData typedData;
    private float applied;
    // mulches its plant's kills dropped this level (their own drop rolls, not the wave's guaranteed one)
    private int drops;

    public ScavengerMulchEffect(ScavengerMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.mulchDropRate);
        Owner.mulchDropRate += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.mulchDropRate -= applied;
        applied = 0f;
    }

    public override void OnKillDrop(MulchItem item, bool fromChance)
    {
        if (fromChance) drops++;
    }

    public override string GetStatus() => $"Mulches its kills dropped: {drops}";
}
