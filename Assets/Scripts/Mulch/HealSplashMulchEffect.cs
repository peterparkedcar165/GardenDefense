using System.Collections.Generic;
using UnityEngine;

// whenever a plant heals ANOTHER plant (not itself, and not a pure regeneration-over-time tick -
// see HealTag.Regeneration/StatusEffect.HealTarget), deals damage to insects near the healed
// plant equal to a percent of the healing done, using the healer's own damage type/element.
// gated by a per-healer internal cooldown so a fast-ticking heal can't spam it
public class HealSplashMulchEffect : MulchEffect
{
    private readonly HealSplashMulchData typedData;
    private readonly Dictionary<Plant, float> lastTriggerTime = new Dictionary<Plant, float>();

    public HealSplashMulchEffect(HealSplashMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => Entity.OnHeal += HandleHeal;
    public override void Deactivate()
    {
        Entity.OnHeal -= HandleHeal;
        lastTriggerTime.Clear();
    }

    private void HandleHeal(EntityEventData heal)
    {
        if (Entity.HasHealTag(heal.healTags, HealTag.Regeneration)) return;
        if (heal.source is not Plant healer || heal.target is not Plant healed || healer == healed) return;

        if (lastTriggerTime.TryGetValue(healer, out float last) && Time.time - last < typedData.internalCooldown) return;
        lastTriggerTime[healer] = Time.time;

        float damage = heal.amount * typedData.damagePercent;
        if (damage <= 0f) return;

        Vector3 center = healed.transform.position;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(insect.transform.position, center) > typedData.radius) continue;
            insect.Damage(damage, healer.damageType, healer.elementalType, healer, false,
                new DamageTag[] { DamageTag.AoE, DamageTag.PassiveDamage });
        }
    }
}
