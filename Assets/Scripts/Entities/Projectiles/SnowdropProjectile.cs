using UnityEngine;

public class SnowdropProjectile : Projectile
{
    private static readonly DamageTag[] attackTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget };

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, attackTags);

        if (!insect.IsAlive) return;
        (source as Snowdrop)?.OnIceAttackHit(insect);
    }

    protected override void Move()
    {
        base.Move();

        // sprite art faces left by default (angle 180), same convention as SunflowerProjectile -
        // the rotation needed to point it along the current heading is the heading's own angle
        // minus that 180 offset. only Visual rotates - Base stays unrotated so Shadow (also a
        // child of Base) doesn't spin along with it. reads VisualHeading (not "direction") so the
        // sprite actually tilts up/down while climbing or diving toward a flying target instead
        // of staying flat
        Vector3 heading = VisualHeading;
        float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
    }
}
