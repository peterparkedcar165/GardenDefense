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
        foreach (Insect splashTarget in new List<Insect>(Insect.allInsects))
        {
            if (splashTarget == null || !splashTarget.IsAlive || splashTarget == insect) continue;
            if (Vector3.Distance(transform.position, splashTarget.transform.position) > anemone.SplashRadius) continue;
            splashTarget.Damage(splashDmg, damageType, elementalType, source, true,
                new DamageTag[] { DamageTag.AoE });
        }
    }
}
