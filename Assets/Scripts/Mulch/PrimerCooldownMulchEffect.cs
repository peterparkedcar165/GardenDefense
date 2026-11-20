using UnityEngine;

// Plant.mulchPrimerCooldownMultiplier: Entity.Damage multiplies an insect's element cooldown by it
// whenever this plant is the one landing the primer (MulchManager.PrimerCooldownFactor)
public class PrimerCooldownMulchEffect : MulchEffect
{
    private readonly PrimerCooldownMulchData typedData;
    private float factor = 1f;

    public PrimerCooldownMulchEffect(PrimerCooldownMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        factor = 1f - Mathf.Clamp(V(typedData.cooldownReductionPercent), 0f, 0.9f);
        Owner.mulchPrimerCooldownMultiplier *= factor;
    }

    public override void Deactivate()
    {
        if (Owner != null && factor > 0f) Owner.mulchPrimerCooldownMultiplier /= factor;
        factor = 1f;
    }
}
