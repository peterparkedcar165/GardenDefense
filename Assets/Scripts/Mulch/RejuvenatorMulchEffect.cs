// boosts every Regeneration-tagged heal board-wide via MulchManager.RegenerationHealMultiplier -
// same static-multiplier convention as PrimerCooldownMulchEffect/SlowBonusMulchEffect/
// RekindlingMulchEffect's own duration shrink. the actual plant-only gating (an insect getting
// buffed, e.g. a Glowworm topping up another insect, is never affected) lives in Entity.Heal()
// itself, not here - see HealTag.Regeneration/StatusEffect.HealTarget for how a heal earns the tag
public class RejuvenatorMulchEffect : MulchEffect
{
    private readonly RejuvenatorMulchData typedData;

    public RejuvenatorMulchEffect(RejuvenatorMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => MulchManager.RegenerationHealMultiplier *= (1f + typedData.effectivenessBonus);
    public override void Deactivate() => MulchManager.RegenerationHealMultiplier /= (1f + typedData.effectivenessBonus);
}
