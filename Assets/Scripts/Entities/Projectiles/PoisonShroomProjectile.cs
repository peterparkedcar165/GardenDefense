using UnityEngine;
using System.Collections.Generic;

public class PoisonShroomProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget };
    // nearest first, by distances worked out once per insect
    private static readonly System.Comparison<(Insect insect, float distance)> ByDistance =
        (a, b) => a.distance.CompareTo(b.distance);

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void OnHit(Insect insect)
    {
        // deals no direct damage; all damage comes from the Toxic Spore DoT applied below
        insect.Damage(0f, damageType, elementalType, source, true, HitTags);

        PoisonShroom cs = source as PoisonShroom;
        if (cs == null) return;

        insect.ApplyEffect(new ToxicSporeEffect(insect, cs.ToxicSporeDuration, 1, source));

        if (cs.IsPath1Maxed)
        {
            // pooled instead of a new list per hit; each insect's distance is worked out once
            // (the sort used to work out two per comparison) and sorted by the same numbers, so
            // the same nearest insects are picked
            List<(Insect insect, float distance)> splashTargets = UnityEngine.Pool.ListPool<(Insect, float)>.Get();
            try
            {
                Vector3 center = insect.transform.position;
                foreach (Insect nearby in Insect.allInsects)
                {
                    if (nearby == null || !nearby.IsAlive || nearby == insect) continue;
                    float distance = Vector3.Distance(center, nearby.transform.position);
                    if (distance <= 1f)
                        splashTargets.Add((nearby, distance));
                }

                // splash only reaches the nearest N OTHER insects (data-driven, tunable per plant) -
                // the main target above already got its Toxic Spore unconditionally, unaffected by this cap
                int maxSplashTargets = (cs.data as PoisonShroomData)?.path1MaxSplashTargets ?? 2;
                if (splashTargets.Count > maxSplashTargets)
                {
                    splashTargets.Sort(ByDistance);
                    splashTargets.RemoveRange(maxSplashTargets, splashTargets.Count - maxSplashTargets);
                }

                for (int i = 0; i < splashTargets.Count; i++)
                {
                    Insect nearby = splashTargets[i].insect;
                    nearby.ApplyEffect(new ToxicSporeEffect(nearby, cs.ToxicSporeDuration, 1, source));
                }
            }
            finally
            {
                UnityEngine.Pool.ListPool<(Insect, float)>.Release(splashTargets);
            }
        }
    }
}
