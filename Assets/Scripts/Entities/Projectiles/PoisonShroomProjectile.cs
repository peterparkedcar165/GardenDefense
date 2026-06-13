using UnityEngine;

public class PoisonShroomProjectile : Projectile
{
    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });

        PoisonShroom ps = source as PoisonShroom;
        if (ps == null) return;

        float additionalDPS = ps.PoisonBaseDPS + (6f * ps.effectivePath2Level) + ps.skillDamageMultiplier * ps.magicPower;
        insect.ApplyEffect(new PoisonEffect(insect, ps.poisonDuration, ps.poisonLevel, source, additionalDPS));

        if (ps.IsPath1Maxed)
        {
            foreach (Insect nearby in new System.Collections.Generic.List<Insect>(Insect.allInsects))
            {
                if (nearby == null || !nearby.IsAlive || nearby == insect) continue;
                if (Vector3.Distance(insect.transform.position, nearby.transform.position) <= 1.5f)
                {
                    nearby.Damage(projectileDamage, damageType, elementalType, source, false, new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
                    nearby.ApplyEffect(new PoisonEffect(nearby, ps.poisonDuration, ps.poisonLevel, source, additionalDPS));
                }
            }
        }
    }
    
}
