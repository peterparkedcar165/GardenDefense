using UnityEngine;

public class AcornProjectile : Projectile
{
    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnHit(Insect insect) // to change for every plant
    {
        
        // if (source != null)
        //    insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag [] {DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget});

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        /* SPECIAL EFFECT */
        
        // checking the passive level
        if (source != null && source is AcornSprout acorn)
        {
             float procChance = acorn.stunChance * (1 + acorn.bonusEffectChance);
             if (Random.value < procChance)
             {
                 insect.ApplyEffect(new StunEffect(
                    insect /*target*/,
                    acorn.stunDuration/*duration in seconds*/,
                    1/*level*/,
                    source/*source*/));
             }
        }
    }
    
}
