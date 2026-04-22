using UnityEngine;

public class PoisonShroomProjectile : Projectile
{

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnHit(Insect insect) // to change for every plant
    {
        
        if (owner != null)
            insect.RegisterAttacker(owner);

        insect.Damage(projectileDamage * (1 - insect.poisonResistance), damageType);

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        PoisonShroom shooter = owner as PoisonShroom;


        /* SPECIAL EFFECT */

        if (shooter != null) 
        {
            int newPoisonLevel;
            if(shooter.passiveLevel <= 0)
            {
                newPoisonLevel = 1;
            } else
            {
                newPoisonLevel = 1 + shooter.passiveLevel;
            }

            insect.ApplyEffect(new PoisonEffect(insect, 4f, newPoisonLevel, shooter));

        } else { 
            return;
        }
    }
    
}
