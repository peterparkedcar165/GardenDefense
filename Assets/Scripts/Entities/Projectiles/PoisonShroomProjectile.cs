using UnityEngine;

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
            foreach (Insect nearby in new System.Collections.Generic.List<Insect>(Insect.allInsects))
            {
                if (nearby == null || !nearby.IsAlive || nearby == insect) continue;
                if (Vector3.Distance(insect.transform.position, nearby.transform.position) <= 1.5f)
                    nearby.ApplyEffect(new ToxicSporeEffect(nearby, cs.ToxicSporeDuration, 1, source));
            }
        }
    }
}
