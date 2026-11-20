// a level wide switch read by Entity.Damage's freeze and fire branch
public class MeltFertilizerEffect : FertilizerEffect
{
    private readonly MeltFertilizerData typedData;

    public MeltFertilizerEffect(MeltFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        FertilizerManager.MeltActive = true;
        FertilizerManager.MeltFreezeDurationReduction = typedData.freezeDurationReduction;
        FertilizerManager.MeltDamageMultiplier = typedData.damageMultiplier;
    }

    public override void Deactivate()
    {
        FertilizerManager.MeltActive = false;
        FertilizerManager.MeltFreezeDurationReduction = 0f;
        FertilizerManager.MeltDamageMultiplier = 1f;
    }
}
