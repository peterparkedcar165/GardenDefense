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
        // the current heading is the heading's own angle minus that 180 offset. only Visual
        // rotates - Base stays unrotated so Shadow (also a child of Base) doesn't spin with it.
        // reads VisualHeading (not "direction") so the sprite tilts up/down while climbing or
        // diving toward a flying target instead of staying flat
        Vector3 heading = VisualHeading;
        float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
    }

    // particle-trail detach-before-destroy is now handled generically by the base class
    // (Projectile.OnBeforeDestroy), for every child particle system automatically
}
