using UnityEngine;

public class SunflowerProjectile : Projectile
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

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] {DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile});
        
        Sunflower sunflower = source as Sunflower;

        if (sunflower != null)
            sunflower.ReduceSunTimer();

        // hit particle spawn is now handled automatically by the base class right after OnHit()
    }

    protected override void Move()
    {
        base.Move();

        // sprite art faces left by default (angle 180), so the rotation needed to point it along
        // the current direction is the direction's own angle minus that 180 offset
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
        }
    }

    // particle-trail detach-before-destroy is now handled generically by the base class
    // (Projectile.OnBeforeDestroy), for every child particle system automatically
}
