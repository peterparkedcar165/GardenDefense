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
        
        //if (source != null)
        //   insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag [] {DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget});

        if (source != null && source is PoisonShroom shooter)
        {
            insect.ApplyEffect(new PoisonEffect(insect, shooter.poisonDuration, shooter.poisonLevel, source, shooter.magicPower * 0.06f));
        }
    }
    
}
