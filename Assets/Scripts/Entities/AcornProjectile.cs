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
        
        if (source != null)
            insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source);

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        /* SPECIAL EFFECT */
        
        // checking the passive level
        if (source != null && source.passiveLevel > 0)
        {
             float procChance = 0.33f * (1 + source.bonusEffectChance);
             if (Random.value < procChance)
             {
                 insect.ApplyEffect(new StunEffect(
                    insect /*target*/,
                    1f/*duration in seconds*/,
                    1/*level*/,
                    source/*source*/));
             }
        }
    }
    
}
