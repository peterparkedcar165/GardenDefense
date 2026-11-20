public class GardenFenceFertilizerEffect : FertilizerEffect
{
    private readonly GardenFenceFertilizerData typedData;

    public GardenFenceFertilizerEffect(GardenFenceFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => FertilizerManager.LeakDamageMultiplier -= typedData.leakDamageReduction;
    public override void Deactivate() => FertilizerManager.LeakDamageMultiplier += typedData.leakDamageReduction;
}
