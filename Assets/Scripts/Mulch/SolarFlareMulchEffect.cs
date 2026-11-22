using System.Collections.Generic;
using UnityEngine;

// hears its own plant's Plant.GenerateSun (the sun actually banked), then hits every insect within its attack range
// through the normal Entity.Damage path with the plant as its source. only does anything on a plant
// that makes sun (sun sap counts)
public class SolarFlareMulchEffect : MulchEffect
{
    private static readonly DamageTag[] FlareTags = { DamageTag.AoE, DamageTag.PassiveDamage };
    private static readonly DamageTag[] FlareTagsNoPrimer = { DamageTag.AoE, DamageTag.PassiveDamage, DamageTag.NoPrimer, DamageTag.ElementalDebuff };

    private readonly SolarFlareMulchData typedData;
    // what its flares took off insects this level
    private float dealt;

    public SolarFlareMulchEffect(SolarFlareMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate() {}

    public override string GetStatus() => $"Flare damage dealt: {dealt:0}";
    public override void Deactivate() {}

    public override void OnSunGenerated(int amount, bool fromKill)
    {
        if (!OwnerAlive || amount <= 0) return;
        if (fromKill && !typedData.triggeredByKillSun) return;
        float damage = amount * V(typedData.damagePercentOfSun);
        if (damage <= 0f) return;

        // the plant's own attack range (Tommy, 2026-11-21), so range stats and long stem widen it
        float radius = Owner.attackRange;
        ElementalType element = typedData.useSourceElement ? Owner.elementalType : typedData.fixedElement;
        DamageTag[] tags = typedData.appliesPrimer ? FlareTags : FlareTagsNoPrimer;
        Vector3 center = Owner.transform.position;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(center, insect.transform.position) > radius) continue;
            dealt += DealDamage(insect, damage, typedData.damageType, element, Owner, tags);
        }
    }
}
