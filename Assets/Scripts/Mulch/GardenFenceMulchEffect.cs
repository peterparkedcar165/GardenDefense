public class GardenFenceMulchEffect : MulchEffect
{
    private readonly GardenFenceMulchData typedData;

    public GardenFenceMulchEffect(GardenFenceMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => MulchManager.LeakDamageMultiplier -= typedData.leakDamageReduction;
    public override void Deactivate() => MulchManager.LeakDamageMultiplier += typedData.leakDamageReduction;
}
