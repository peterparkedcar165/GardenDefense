// shortens every elemental Primer's internal cooldown (Entity.internalCooldown, which every
// per-element cooldown - fire/water/ice/wind/grass/poison - is derived from, see
// Entity.ElementInternalCooldown) via the global MulchManager.PrimerCooldownMultiplier. read
// live every UpdateStats tick, so this covers every insect already on the field and every one
// spawned after the pick with no per-instance hook needed
public class PrimerCooldownMulchEffect : MulchEffect
{
    private readonly PrimerCooldownMulchData typedData;

    public PrimerCooldownMulchEffect(PrimerCooldownMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => MulchManager.PrimerCooldownMultiplier *= (1f - typedData.cooldownReductionPercent);
    public override void Deactivate() => MulchManager.PrimerCooldownMultiplier /= (1f - typedData.cooldownReductionPercent);
}
