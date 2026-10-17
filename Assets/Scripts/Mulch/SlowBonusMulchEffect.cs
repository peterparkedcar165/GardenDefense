// adds a flat extra Movement Speed reduction to every effect that slows insects (SlowEffect,
// MireSlowEffect, OffPathSlownessEffect, SnowMarkEffect's Chilling Grasp slow, SoakedEffect's
// movement component, FrostbiteEffect) via the global MulchManager.GlobalSlowBonus, which each
// of those bakes into its own applied amount at construction time. only affects slows applied
// after this mulch is picked - already-active ones keep their original amount until they refresh
public class SlowBonusMulchEffect : MulchEffect
{
    private readonly SlowBonusMulchData typedData;

    public SlowBonusMulchEffect(SlowBonusMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => MulchManager.GlobalSlowBonus += typedData.flatBonus;
    public override void Deactivate() => MulchManager.GlobalSlowBonus -= typedData.flatBonus;
}
