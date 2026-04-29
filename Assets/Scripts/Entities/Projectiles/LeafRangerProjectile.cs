using UnityEngine;

public class LeafRangerProjectile : Projectile
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
        //    insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag [] {DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget});

        if (piercing > 0)
        {
            trackedTarget = null;
            piercing--;
        } else
        {
            Destroy(gameObject);
         }


        /* SPECIAL EFFECT */
        
        // checking the passive level
        //if (source != null && source.passiveLevel > 0)
        //{
            // for now, nothing
        //}
    }
    
}
