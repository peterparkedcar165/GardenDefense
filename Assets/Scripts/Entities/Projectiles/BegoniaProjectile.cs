using UnityEngine;

public class BegoniaProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget };

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            HitTags);
    }
}
