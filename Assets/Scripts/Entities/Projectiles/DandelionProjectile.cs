using UnityEngine;

public class DandelionProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget };

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true, HitTags);
        PlaySound(hit);
        trackedTarget = null;
        // rolled independently for every insect the pierce hits, not just the first
        (source as Dandelion)?.TryAcceleratePassive(insect);
    }
}
