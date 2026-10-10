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
        // the rotation needed to point it along the current direction is the direction's own
        // angle minus that 180 offset
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
        }
    }
}
