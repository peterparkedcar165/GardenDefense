using System.Buffers;
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
        float radiusSqr = radius * radius;

        // a snapshot of the insects, since a flare's damage can kill or spawn insects. rented, not
        // a new list per sun event; a sun event inside this loop (alchemy paid by a reaction the
        // flare set off) rents its own array, so the nested flare never touches this one
        List<Insect> insects = Insect.allInsects;
        int count = insects.Count;
        Insect[] snapshot = ArrayPool<Insect>.Shared.Rent(Mathf.Max(1, count));
        insects.CopyTo(snapshot, 0);
        try
        {
            for (int i = 0; i < count; i++)
            {
                Insect insect = snapshot[i];
                if (insect == null || !insect.IsAlive) continue;
                if ((insect.transform.position - center).sqrMagnitude > radiusSqr) continue;
                dealt += DealDamage(insect, damage, typedData.damageType, element, Owner, tags);
            }
        }
        finally
        {
            // cleared so the shared pool doesn't keep dead insects alive
            System.Array.Clear(snapshot, 0, count);
            ArrayPool<Insect>.Shared.Return(snapshot);
        }
    }
}
