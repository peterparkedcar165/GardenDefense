using UnityEngine;

public class CordycepsProjectile : Projectile
{
    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        Cordyceps cs = source as Cordyceps;
        if (cs == null) return;

        insect.ApplyEffect(new DecayEffect(insect, cs.invertDuration, cs.invertLevel, source));

        if (cs.IsPath1Maxed)
        {
            foreach (Insect nearby in new System.Collections.Generic.List<Insect>(Insect.allInsects))
            {
                if (nearby == null || !nearby.IsAlive || nearby == insect) continue;
                if (Vector3.Distance(insect.transform.position, nearby.transform.position) <= 1.5f)
                {
                    nearby.Damage(projectileDamage, damageType, elementalType, source, false, new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
                    nearby.ApplyEffect(new DecayEffect(nearby, cs.invertDuration, cs.invertLevel, source));
                }
            }
        }
    }
}
