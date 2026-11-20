// Plant.mulchRegenerationBonus, read by Entity.Heal for regeneration tagged heals this plant gives
// (MulchManager.RegenerationFactor). a plant's own self regeneration counts too
public class RejuvenatorMulchEffect : MulchEffect
{
    private readonly RejuvenatorMulchData typedData;
    private float applied;

    public RejuvenatorMulchEffect(RejuvenatorMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.effectivenessBonus);
        Owner.mulchRegenerationBonus += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.mulchRegenerationBonus -= applied;
        applied = 0f;
    }
}
