public class ScavengerMulchEffect : MulchEffect
{
    private readonly ScavengerMulchData typedData;
    private float applied;

    public ScavengerMulchEffect(ScavengerMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.itemDropRate);
        Owner.itemDropRate += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.itemDropRate -= applied;
        applied = 0f;
    }
}
