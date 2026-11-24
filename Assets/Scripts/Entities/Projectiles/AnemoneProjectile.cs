using UnityEngine;
using System.Collections.Generic;

public class AnemoneProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into them
    private static readonly DamageTag[] HitTags = { DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile };
    private static readonly DamageTag[] SplashTags = { DamageTag.AoE };
    // nearest first, by distances worked out once per insect
    private static readonly System.Comparison<(Insect insect, float distance)> ByDistance =
        (a, b) => a.distance.CompareTo(b.distance);

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, HitTags);

        Anemone anemone = source as Anemone;
        if (anemone == null) return;

        float splashDmg = projectileDamage * 0.5f;
        // pooled instead of a new list per hit; each insect's distance is worked out once (the
        // sort used to work out two per comparison) and sorted by the same numbers, so the same
        // nearest insects are picked
        List<(Insect insect, float distance)> splashTargets = UnityEngine.Pool.ListPool<(Insect, float)>.Get();
        try
        {
            Vector3 center = transform.position;
            foreach (Insect splashTarget in Insect.allInsects)
            {
                if (splashTarget == null || !splashTarget.IsAlive || splashTarget == insect) continue;
                float distance = Vector3.Distance(center, splashTarget.transform.position);
                if (distance > anemone.SplashRadius) continue;
                splashTargets.Add((splashTarget, distance));
            }

            // splash only hits the nearest N insects (data-driven, tunable per plant) - the main
            // target above is unaffected by this cap, it always takes full damage
            int maxExtraTargets = (anemone.data as AnemoneData)?.splashMaxExtraTargets ?? 3;
            if (splashTargets.Count > maxExtraTargets)
            {
                splashTargets.Sort(ByDistance);
                splashTargets.RemoveRange(maxExtraTargets, splashTargets.Count - maxExtraTargets);
            }

            for (int i = 0; i < splashTargets.Count; i++)
                splashTargets[i].insect.Damage(splashDmg, damageType, elementalType, source, true, SplashTags);
        }
        finally
        {
            UnityEngine.Pool.ListPool<(Insect, float)>.Release(splashTargets);
        }
    }
}
