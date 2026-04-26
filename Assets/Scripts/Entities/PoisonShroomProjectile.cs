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

    protected override void OnHit(Insect insect) // to change for every plant
    {
        
        if (source != null)
            insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag [] {DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget});

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        /* SPECIAL EFFECT */

        if (source != null) 
        {
            int newPoisonLevel;
            if(source.passiveLevel <= 0)
            {
                newPoisonLevel = 1;
            } else
            {
                newPoisonLevel = 1 + source.passiveLevel;
            }

            insect.ApplyEffect(new PoisonEffect(insect, 8f, newPoisonLevel, source));

        } else { 
            return;
        }
    }
    
}
