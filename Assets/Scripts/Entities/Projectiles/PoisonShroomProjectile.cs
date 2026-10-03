using UnityEngine;
using System.Collections.Generic;

public class PoisonShroomProjectile : Projectile
{
    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void OnHit(Insect insect)
    {
        // deals no direct damage; all damage comes from the Toxic Spore DoT applied below
        insect.Damage(0f, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        PoisonShroom cs = source as PoisonShroom;
        if (cs == null) return;

        insect.ApplyEffect(new ToxicSporeEffect(insect, cs.ToxicSporeDuration, 1, source));

        if (cs.IsPath1Maxed)
        {
            List<Insect> splashTargets = new List<Insect>();
            foreach (Insect nearby in Insect.allInsects)
            {
                if (nearby == null || !nearby.IsAlive || nearby == insect) continue;
                if (Vector3.Distance(insect.transform.position, nearby.transform.position) <= 1f)
                    splashTargets.Add(nearby);
            }

            // splash only reaches the nearest N OTHER insects (data-driven, tunable per plant) -
            // the main target above already got its Toxic Spore unconditionally, unaffected by this cap
            int maxSplashTargets = (cs.data as PoisonShroomData)?.path1MaxSplashTargets ?? 2;
            if (splashTargets.Count > maxSplashTargets)
            {
                splashTargets.Sort((a, b) =>
                    Vector3.Distance(insect.transform.position, a.transform.position).CompareTo(Vector3.Distance(insect.transform.position, b.transform.position)));
                splashTargets.RemoveRange(maxSplashTargets, splashTargets.Count - maxSplashTargets);
            }

            foreach (Insect nearby in splashTargets)
                nearby.ApplyEffect(new ToxicSporeEffect(nearby, cs.ToxicSporeDuration, 1, source));
        }
    }
}
