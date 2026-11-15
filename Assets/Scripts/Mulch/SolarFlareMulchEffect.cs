using System.Collections.Generic;
using UnityEngine;

// hears every Plant.GenerateSun call (MulchManager.NotifySunGenerated) with the sun actually
// banked, then hits every insect within range of that plant. the damage goes through the normal
// Entity.Damage path with the sun plant as its source, so its stats, dormancy and resistances all
// apply, and an elemental flare primes insects the same way any elemental hit does
public class SolarFlareMulchEffect : MulchEffect
{
    private static readonly DamageTag[] FlareTags = { DamageTag.AoE, DamageTag.PassiveDamage };
    private static readonly DamageTag[] FlareTagsNoPrimer = { DamageTag.AoE, DamageTag.PassiveDamage, DamageTag.NoPrimer, DamageTag.ElementalDebuff };

    private readonly SolarFlareMulchData typedData;

    public SolarFlareMulchEffect(SolarFlareMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() {}

    public override void OnSunGenerated(Plant plant, int amount, bool fromKill)
    {
        if (plant == null || amount <= 0) return;
        if (fromKill && !typedData.triggeredByKillSun) return;

        float damage = amount * typedData.damagePercentOfSun;
        if (damage <= 0f) return;

        float radius = typedData.radiusTiles * Tile.TileSize;
        ElementalType element = typedData.useSourceElement ? plant.elementalType : typedData.fixedElement;
        DamageTag[] tags = typedData.appliesPrimer ? FlareTags : FlareTagsNoPrimer;
        Vector3 center = plant.transform.position;

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(center, insect.transform.position) > radius) continue;
            insect.Damage(damage, typedData.damageType, element, plant, false, tags);
        }
    }
}
