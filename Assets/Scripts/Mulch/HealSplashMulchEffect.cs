using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// whenever its plant heals another plant (not itself, and not a regeneration tick), insects near
// the healed plant take a share of the healing as damage, in the healer's own damage type and
// element. the damage lands 0.1 s after the heal, like other on hit effects
public class HealSplashMulchEffect : MulchEffect
{
    private const float OnHitDelay = 0.1f;
    private static readonly DamageTag[] SplashTags = { DamageTag.AoE, DamageTag.PassiveDamage };

    private readonly HealSplashMulchData typedData;
    private float lastTrigger = float.NegativeInfinity;
    private bool listening;
    // what its splashes took off insects this level
    private float dealt;

    public HealSplashMulchEffect(HealSplashMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (listening) return;
        listening = true;
        Entity.OnHeal += HandleHeal;
    }

    public override void Deactivate()
    {
        if (!listening) return;
        listening = false;
        Entity.OnHeal -= HandleHeal;
    }

    public override string GetStatus() => $"Splash damage dealt: {dealt:0}";

    private void HandleHeal(EntityEventData heal)
    {
        if (!OwnerAlive || heal.source != Owner) return;
        if (Entity.HasHealTag(heal.healTags, HealTag.Regeneration)) return;
        if (heal.target is not Plant healed || healed == Owner) return;
        if (Time.time - lastTrigger < typedData.internalCooldown) return;
        lastTrigger = Time.time;

        float damage = heal.amount * V(typedData.damagePercent);
        if (damage > 0f) StartOnOwner(DelayedSplash(healed, damage));
    }

    private IEnumerator DelayedSplash(Plant healed, float damage)
    {
        yield return new WaitForSeconds(OnHitDelay);
        if (!OwnerAlive) yield break;
        Vector3 center = healed != null && healed.IsAlive ? healed.transform.position : Owner.transform.position;
        float radius = typedData.radius * Tile.CellSize;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(insect.transform.position, center) > radius) continue;
            dealt += DealDamage(insect, damage, Owner.damageType, Owner.elementalType, Owner, SplashTags);
        }
    }
}
