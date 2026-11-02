// global toggle read directly by Entity.cs's Freeze+Fire branch, same static-field convention as
// RejuvenatorMulchEffect/RekindlingMulchEffect - no per-plant hook needed, the reaction already
// lives entirely inside Entity.Damage()
public class MeltMulchEffect : MulchEffect
{
    private readonly MeltMulchData typedData;

    public MeltMulchEffect(MeltMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        MulchManager.MeltActive = true;
        MulchManager.MeltFreezeDurationReduction = typedData.freezeDurationReduction;
        MulchManager.MeltDamageMultiplier = typedData.damageMultiplier;
    }

    public override void Deactivate()
    {
        MulchManager.MeltActive = false;
        MulchManager.MeltFreezeDurationReduction = 0f;
        MulchManager.MeltDamageMultiplier = 1f;
    }
}
