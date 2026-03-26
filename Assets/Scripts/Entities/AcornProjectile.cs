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

    protected override void OnHit(Insect insectHit)
    {
        base.OnHit(insectHit); // will handle damage, registration and pierce/destroy

        AcornSprout shooter = owner as AcornSprout;

        // checking the passive level
        if (shooter != null && shooter.passiveLevel > 0)
        {
             float procChance = 0.33f * (1 + shooter.bonusEffectChance);
             if (Random.value < procChance)
             {
                 insectHit.ApplyEffect(new StunEffect(
                    insectHit /*target*/,
                    1f/*duration in seconds*/,
                    1/*level*/,
                    shooter/*source*/));
             }
        }
    }
    
}
