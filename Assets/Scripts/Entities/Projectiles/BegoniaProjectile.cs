using UnityEngine;

public class BegoniaProjectile : Projectile
{
    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
