using UnityEngine;

public class AcornProjectile : Projectile
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

        insect.Damage(projectileDamage * (1 - insect.natureResistance), damageType);

        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
         }

        AcornSprout shooter = owner as AcornSprout;


        /* SPECIAL EFFECT */
        
        // checking the passive level
        if (shooter != null && shooter.passiveLevel > 0)
        {
             float procChance = 0.33f * (1 + shooter.bonusEffectChance);
             if (Random.value < procChance)
             {
                 insect.ApplyEffect(new StunEffect(
                    insect /*target*/,
                    1f/*duration in seconds*/,
                    1/*level*/,
                    shooter/*source*/));
             }
        }
    }
    
}
