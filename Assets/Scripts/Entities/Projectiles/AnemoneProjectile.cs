using UnityEngine;
using System.Collections.Generic;

public class AnemoneProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile });

        Anemone anemone = source as Anemone;
        if (anemone == null) return;

        float splashDmg = projectileDamage * 0.5f;
        List<Insect> splashTargets = new List<Insect>();
        foreach (Insect splashTarget in Insect.allInsects)
        {
            if (splashTarget == null || !splashTarget.IsAlive || splashTarget == insect) continue;
            if (Vector3.Distance(transform.position, splashTarget.transform.position) > anemone.SplashRadius) continue;
            splashTargets.Add(splashTarget);
        }

        // splash only hits the nearest N insects (data-driven, tunable per plant) - the main
        // target above is unaffected by this cap, it always takes full damage
        int maxExtraTargets = (anemone.data as AnemoneData)?.splashMaxExtraTargets ?? 3;
        if (splashTargets.Count > maxExtraTargets)
        {
            splashTargets.Sort((a, b) =>
                Vector3.Distance(transform.position, a.transform.position).CompareTo(Vector3.Distance(transform.position, b.transform.position)));
            splashTargets.RemoveRange(maxExtraTargets, splashTargets.Count - maxExtraTargets);
        }

        foreach (Insect splashTarget in splashTargets)
            splashTarget.Damage(splashDmg, damageType, elementalType, source, true,
                new DamageTag[] { DamageTag.AoE });
    }
}
