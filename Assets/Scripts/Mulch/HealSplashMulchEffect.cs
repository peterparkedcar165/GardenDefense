using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// whenever a plant heals ANOTHER plant (not itself, and not a pure regeneration-over-time tick -
// see HealTag.Regeneration/StatusEffect.HealTarget), deals damage to insects near the healed
// plant equal to a percent of the healing done, using the healer's own damage type/element.
// gated by a per-healer internal cooldown so a fast-ticking heal can't spam it. the damage itself
// lands 0.1s after the heal, same delay convention as other on-hit effects (e.g. Bird of
// Paradise's DelayedExtraProc) - hosted on MulchManager's MonoBehaviour since this effect has no
// Plant/MonoBehaviour of its own to run a coroutine on
public class HealSplashMulchEffect : MulchEffect
{
    private const float OnHitDelay = 0.1f;

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

        if (MulchManager.instance != null)
            MulchManager.instance.StartCoroutine(DelayedSplash(healer, healed, damage));
    }

    private IEnumerator DelayedSplash(Plant healer, Plant healed, float damage)
    {
        yield return new WaitForSeconds(OnHitDelay);
        if (healer == null || !healer.IsAlive) yield break;

        Vector3 center = (healed != null && healed.IsAlive) ? healed.transform.position : healer.transform.position;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(insect.transform.position, center) > typedData.radius) continue;
            insect.Damage(damage, healer.damageType, healer.elementalType, healer, false,
                new DamageTag[] { DamageTag.AoE, DamageTag.PassiveDamage });
        }
    }
}
